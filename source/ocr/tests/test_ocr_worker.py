"""
Unit tests for the OCR extraction worker.

Tests are isolated from Redis using mocks — no live infrastructure required.
Sample images are generated in-memory using Pillow.
"""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any
from unittest.mock import AsyncMock, MagicMock, patch

import cv2
import numpy as np
import pytest
from PIL import Image, ImageDraw, ImageFont

from src.config import Settings
from src.ocr_worker import OcrWorker


# ── Fixtures ───────────────────────────────────────────────────────────────────


@pytest.fixture()
def settings(tmp_path: Path) -> Settings:
    return Settings(
        redis_url="redis://localhost:6379",
        api_base_url="http://localhost:5000",
        storage_receipts_path=str(tmp_path / "receipts"),
        storage_thumbnails_path=str(tmp_path / "thumbnails"),
        storage_ocr_json_path=str(tmp_path / "ocr-json"),
    )


@pytest.fixture()
def worker(settings: Settings) -> OcrWorker:
    return OcrWorker(settings)


def make_receipt_image(
    tmp_path: Path,
    text_lines: list[str],
    name: str = "receipt.jpg",
) -> Path:
    """Generate a synthetic receipt image with rendered text for testing."""
    width, height = 600, 900
    img = Image.new("RGB", (width, height), color=(255, 255, 255))
    draw = ImageDraw.Draw(img)

    y = 20
    for line in text_lines:
        draw.text((20, y), line, fill=(0, 0, 0))
        y += 30

    p = tmp_path / name
    img.save(p, format="JPEG", quality=95)
    return p


# ── Preprocessing tests ────────────────────────────────────────────────────────


def test_preprocess_returns_binary_image(worker: OcrWorker, tmp_path: Path) -> None:
    """Preprocessed image should be grayscale (2D) with values 0 or 255."""
    src = make_receipt_image(tmp_path, ["Test Receipt", "Total: $10.00"])
    pil_img = Image.open(src).convert("RGB")
    cv_img = cv2.cvtColor(np.array(pil_img), cv2.COLOR_RGB2BGR)

    result = worker._preprocess(cv_img)

    assert result.ndim == 2, "Expected 2D grayscale array"
    unique_values = set(result.flatten().tolist())
    # Adaptive threshold produces binary image — only 0 and 255.
    assert unique_values.issubset({0, 255}), f"Unexpected pixel values: {unique_values}"


def test_preprocess_scales_narrow_image(worker: OcrWorker) -> None:
    """Images narrower than 2100px should be upscaled."""
    narrow = np.ones((200, 400, 3), dtype=np.uint8) * 200
    result = worker._preprocess(narrow)
    # After scaling 400px wide → 2100px, height scales proportionally.
    assert result.shape[1] >= 2100


def test_deskew_does_not_crash_on_blank_image(worker: OcrWorker) -> None:
    """Deskew should return the original image if no lines are detected."""
    blank = np.ones((500, 300), dtype=np.uint8) * 255
    result = worker._deskew(blank)
    assert result.shape == blank.shape


# ── Field extraction tests ─────────────────────────────────────────────────────


@pytest.mark.parametrize(
    "text, expected_iso",
    [
        ("Date: 2026-01-15", "2026-01-15T00:00:00Z"),
        ("15/01/2026", "2026-01-15T00:00:00Z"),
        ("01/15/2026", "2026-01-15T00:00:00Z"),
        ("Jan 15 2026", "2026-01-15T00:00:00Z"),
        ("January 15, 2026", "2026-01-15T00:00:00Z"),
    ],
)
def test_extract_date_various_formats(
    worker: OcrWorker, text: str, expected_iso: str
) -> None:
    date_str, conf = worker._extract_date(text)
    assert date_str == expected_iso, f"For text '{text}' got '{date_str}'"
    assert conf > 0


def test_extract_date_returns_none_when_no_date(worker: OcrWorker) -> None:
    date_str, conf = worker._extract_date("No date here at all.")
    assert date_str is None
    assert conf == 0


@pytest.mark.parametrize(
    "text, expected_total",
    [
        ("Total: $42.50", 42.50),
        ("GRAND TOTAL  $1,234.56", 1234.56),
        ("Amount Due: 99.00", 99.00),
        ("Total Due 15.99", 15.99),
    ],
)
def test_extract_total_known_patterns(
    worker: OcrWorker, text: str, expected_total: float
) -> None:
    import re
    from src.ocr_worker import _TOTAL_PATTERN

    total, conf = worker._extract_amount(_TOTAL_PATTERN, text)
    assert total == pytest.approx(expected_total)
    assert conf > 0


def test_extract_total_returns_none_when_absent(worker: OcrWorker) -> None:
    from src.ocr_worker import _TOTAL_PATTERN

    total, conf = worker._extract_amount(_TOTAL_PATTERN, "no financial data here")
    assert total is None
    assert conf == 0


@pytest.mark.parametrize(
    "text, expected_tax",
    [
        ("Tax: $3.50", 3.50),
        ("GST 1.25", 1.25),
        ("VAT: $8.00", 8.00),
        ("HST 12.50", 12.50),
    ],
)
def test_extract_tax_known_patterns(
    worker: OcrWorker, text: str, expected_tax: float
) -> None:
    from src.ocr_worker import _TAX_PATTERN

    tax, conf = worker._extract_amount(_TAX_PATTERN, text)
    assert tax == pytest.approx(expected_tax)


def test_extract_line_items(worker: OcrWorker) -> None:
    lines = [
        "Milk 2% 2L",           # No price — should be skipped.
        "Apples 2 3.99",         # Matches: name=Apples qty=2 price=3.99
        "Bread 1 2.49",          # Matches
        "Subtotal 6.48",         # Keyword line — no qty field, should be skipped.
    ]
    items = worker._extract_line_items(lines)
    assert len(items) == 2  # noqa: PLR2004
    assert items[0]["name"] == "Apples"
    assert items[0]["quantity"] == pytest.approx(2.0)
    assert items[0]["unitPrice"] == pytest.approx(3.99)
    assert items[1]["name"] == "Bread"


def test_extract_time(worker: OcrWorker) -> None:
    assert worker._extract_time("14:32") == "14:32"
    assert worker._extract_time("2:15 PM") == "2:15 PM"
    assert worker._extract_time("no time here") is None


# ── Raw OCR JSON write ─────────────────────────────────────────────────────────


def test_write_raw_ocr_creates_file(worker: OcrWorker, tmp_path: Path) -> None:
    """_write_raw_ocr should create a JSON file at the configured path."""
    (tmp_path / "ocr-json").mkdir(parents=True, exist_ok=True)

    fake_tess_data: dict[str, Any] = {
        "text": ["Hello", "World"],
        "conf": [95, 88],
        "top": [10, 40],
        "left": [5, 5],
        "width": [50, 50],
        "height": [20, 20],
        "line_num": [1, 2],
    }
    path = worker._write_raw_ocr("receipt-abc", fake_tess_data, "Hello World")

    dest = Path(path)
    assert dest.exists()
    data = json.loads(dest.read_text())
    assert data["fullText"] == "Hello World"
    assert data["text"] == ["Hello", "World"]


# ── Consumer loop — Redis mock ─────────────────────────────────────────────────


@pytest.mark.asyncio()
async def test_process_publishes_result_on_success(
    worker: OcrWorker, tmp_path: Path
) -> None:
    """
    _process should call _publish_result when the pipeline succeeds.
    The pipeline itself is mocked to avoid real Tesseract/OpenCV invocation.
    """
    (tmp_path / "ocr-json").mkdir(parents=True, exist_ok=True)

    receipt_path = make_receipt_image(
        tmp_path, ["SUPERMART", "Total: $25.00", "Date: 2026-01-15"]
    )
    payload = json.dumps({
        "receiptId": "test-receipt-001",
        "filePath": str(receipt_path),
        "userId": "user-123",
        "submittedAt": "2026-01-15T10:00:00Z",
    })

    # Mock _run_pipeline to return a fixed result (avoids real Tesseract dependency).
    fake_result: dict[str, Any] = {
        "receiptId": "test-receipt-001",
        "status": "complete",
        "merchantName": "SUPERMART",
        "total": 25.00,
        "rawOcrPath": str(tmp_path / "ocr-json" / "test-receipt-001.json"),
        "errorMessage": None,
    }

    published: list[tuple[str, dict[str, str]]] = []

    async def fake_xadd(stream: str, fields: dict[str, str]) -> str:
        published.append((stream, fields))
        return "1-0"

    async def fake_xack(*args: Any) -> int:
        return 1

    worker._redis = MagicMock()
    worker._redis.xadd = fake_xadd
    worker._redis.xack = fake_xack

    with patch.object(worker, "_run_pipeline", return_value=fake_result):
        await worker._process("1-0", {"payload": payload})

    assert len(published) == 1
    stream, fields = published[0]
    assert stream == "ocr.results"
    result_data = json.loads(fields["payload"])
    assert result_data["status"] == "complete"
    assert result_data["merchantName"] == "SUPERMART"


@pytest.mark.asyncio()
async def test_process_publishes_error_on_pipeline_failure(
    worker: OcrWorker, tmp_path: Path
) -> None:
    """When _run_pipeline raises, _process should publish an ocr_failed result."""
    payload = json.dumps({
        "receiptId": "receipt-fail",
        "filePath": "/nonexistent/path.jpg",
        "userId": "user-999",
        "submittedAt": "2026-01-15T10:00:00Z",
    })

    published: list[tuple[str, dict[str, str]]] = []

    async def fake_xadd(stream: str, fields: dict[str, str]) -> str:
        published.append((stream, fields))
        return "1-0"

    async def fake_xack(*args: Any) -> int:
        return 1

    worker._redis = MagicMock()
    worker._redis.xadd = fake_xadd
    worker._redis.xack = fake_xack

    # _run_pipeline will raise because the file doesn't exist.
    await worker._process("2-0", {"payload": payload})

    assert len(published) == 1
    _, fields = published[0]
    result_data = json.loads(fields["payload"])
    assert result_data["status"] == "ocr_failed"
    assert result_data["receiptId"] == "receipt-fail"
    assert result_data["errorMessage"] is not None


@pytest.mark.asyncio()
async def test_process_acks_even_on_failure(
    worker: OcrWorker, tmp_path: Path
) -> None:
    """XACK must be called regardless of pipeline outcome."""
    payload = json.dumps({
        "receiptId": "receipt-ack-test",
        "filePath": "/nonexistent.jpg",
        "userId": "u",
        "submittedAt": "2026-01-15T10:00:00Z",
    })

    acked: list[str] = []

    async def fake_xack(stream: str, group: str, message_id: str) -> int:
        acked.append(message_id)
        return 1

    async def fake_xadd(stream: str, fields: dict[str, str]) -> str:
        return "1-0"

    worker._redis = MagicMock()
    worker._redis.xadd = fake_xadd
    worker._redis.xack = fake_xack

    await worker._process("msg-99", {"payload": payload})

    assert "msg-99" in acked, "XACK must be called even when the pipeline fails"
