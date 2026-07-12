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
        # storage_receipts_path/storage_thumbnails_path/storage_ocr_json_path are derived
        # @property values on Settings, not real fields — set the base path instead.
        storage_base_path=str(tmp_path),
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
    path = worker._write_raw_ocr("receipt-abc", fake_tess_data, "Hello World", {})

    dest = Path(path)
    assert dest.exists()
    data = json.loads(dest.read_text())
    assert data["fullText"] == "Hello World"
    assert data["text"] == ["Hello", "World"]
    assert data["fieldRegions"] == {}


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

    # _run_pipeline will raise because the file doesn't exist. Retries wait 10s/30s for
    # real (_RETRY_DELAYS) — patch asyncio.sleep so this test doesn't take 40+ seconds,
    # and each retry attempt also publishes a "processing (retry N of 3)" status message.
    with patch("src.ocr_worker.asyncio.sleep", new=AsyncMock()):
        await worker._process("2-0", {"payload": payload})

    assert len(published) == 3  # 2 retry-status messages + 1 final ocr_failed  # noqa: PLR2004
    _, fields = published[-1]
    result_data = json.loads(fields["payload"])
    assert result_data["status"] == "ocr_failed"
    assert result_data["receiptId"] == "receipt-fail"
    assert result_data["errorMessage"] is not None


# ── Template-guided extraction (US-INT-05) ─────────────────────────────────────


def test_box_to_normalized_region(worker: OcrWorker) -> None:
    region = worker._box_to_normalized_region((100, 50, 200, 40), img_w=1000, img_h=1000)
    assert region == {"regionX": 0.1, "regionY": 0.05, "regionW": 0.2, "regionH": 0.04}


def test_fetch_templates_returns_empty_dict_on_failure(worker: OcrWorker) -> None:
    """Network/API failures must not break OCR — fall back to full-image only."""
    with patch("src.ocr_worker.httpx.Client", side_effect=RuntimeError("connection refused")):
        result = worker._fetch_templates("woolworths")
    assert result == {}


def test_fetch_templates_parses_items_keyed_by_field(worker: OcrWorker) -> None:
    fake_response = MagicMock()
    fake_response.json.return_value = {
        "items": [
            {
                "merchantNameNormalized": "woolworths",
                "fieldName": "merchantName",
                "regionX": 0.1, "regionY": 0.05, "regionW": 0.3, "regionH": 0.06,
                "sampleCount": 7,
                "lastUpdated": "2026-09-01T00:00:00Z",
            }
        ]
    }
    fake_response.raise_for_status.return_value = None

    fake_client = MagicMock()
    fake_client.get.return_value = fake_response
    fake_client.__enter__.return_value = fake_client
    fake_client.__exit__.return_value = False

    with patch("src.ocr_worker.httpx.Client", return_value=fake_client):
        result = worker._fetch_templates("woolworths")

    assert "merchantName" in result
    assert result["merchantName"]["sampleCount"] == 7  # noqa: PLR2004


def test_run_targeted_pass_uses_template_region(worker: OcrWorker) -> None:
    """Crop bounds should derive from the template region scaled to image size."""
    preprocessed = np.ones((1000, 800), dtype=np.uint8) * 255
    template: dict[str, Any] = {"regionX": 0.1, "regionY": 0.05, "regionW": 0.3, "regionH": 0.06}

    fake_tess_data = {
        "text": ["SUPERMART", ""],
        "conf": [92, -1],
    }
    with patch("src.ocr_worker.pytesseract.image_to_data", return_value=fake_tess_data):
        name, conf = worker._run_targeted_pass(preprocessed, template, img_w=800, img_h=1000)

    assert name == "SUPERMART"
    assert conf == 92  # noqa: PLR2004


def test_run_targeted_pass_returns_none_when_region_empty(worker: OcrWorker) -> None:
    preprocessed = np.ones((100, 100), dtype=np.uint8) * 255
    # Region outside image bounds collapses to an empty crop.
    template: dict[str, Any] = {"regionX": 1.5, "regionY": 1.5, "regionW": 0.1, "regionH": 0.1}

    name, conf = worker._run_targeted_pass(preprocessed, template, img_w=100, img_h=100)

    assert name is None
    assert conf == 0


def test_run_pipeline_uses_targeted_result_when_more_confident(
    worker: OcrWorker, tmp_path: Path
) -> None:
    """
    End-to-end template-guided extraction: with a stored template (sample_count >= 5)
    and a targeted-pass confidence higher than the full-image pass, the pipeline
    should adopt the targeted merchant name/confidence and log the selection.
    """
    (tmp_path / "ocr-json").mkdir(parents=True, exist_ok=True)
    receipt_path = make_receipt_image(tmp_path, ["supermart", "Total: $25.00", "2026-01-15"])

    # Full-image pass: low-confidence merchant read.
    full_tess_data = {
        "text": ["supermart", "Total:", "$25.00", "2026-01-15"],
        "conf": [55, 90, 90, 90],
        "top": [10, 40, 40, 70],
        "left": [20, 20, 80, 20],
        "width": [90, 40, 50, 90],
        "height": [20, 20, 20, 20],
        "line_num": [1, 2, 2, 3],
    }
    stored_template = {
        "merchantNameNormalized": "supermart",
        "fieldName": "merchantName",
        "regionX": 0.03, "regionY": 0.01, "regionW": 0.15, "regionH": 0.02,
        "sampleCount": 6,
        "lastUpdated": "2026-09-01T00:00:00Z",
    }

    with (
        patch("src.ocr_worker.pytesseract.image_to_data", return_value=full_tess_data),
        patch.object(worker, "_fetch_templates", return_value={"merchantName": stored_template}) as fetch_mock,
        patch.object(worker, "_run_targeted_pass", return_value=("SUPERMART INC", 95)) as targeted_mock,
        patch.object(worker, "_scan_barcode", return_value=(None, None)),
    ):
        result = worker._run_pipeline("receipt-template-test", str(receipt_path))

    fetch_mock.assert_called_once()
    targeted_mock.assert_called_once()
    assert result["merchantName"] == "SUPERMART INC"
    assert result["confidence"]["merchantName"] == 95  # noqa: PLR2004

    # Raw OCR JSON should carry the full-image region so a future confirmation can update the template.
    raw = json.loads(Path(result["rawOcrPath"]).read_text())
    assert "merchantName" in raw["fieldRegions"]


def test_run_pipeline_keeps_full_image_result_when_template_less_confident(
    worker: OcrWorker, tmp_path: Path
) -> None:
    """If the targeted pass is less confident than the full-image pass, keep the full-image result."""
    (tmp_path / "ocr-json").mkdir(parents=True, exist_ok=True)
    receipt_path = make_receipt_image(tmp_path, ["SUPERMART", "Total: $25.00", "2026-01-15"])

    full_tess_data = {
        "text": ["SUPERMART", "Total:", "$25.00", "2026-01-15"],
        "conf": [92, 90, 90, 90],
        "top": [10, 40, 40, 70],
        "left": [20, 20, 80, 20],
        "width": [90, 40, 50, 90],
        "height": [20, 20, 20, 20],
        "line_num": [1, 2, 2, 3],
    }
    stored_template = {
        "merchantNameNormalized": "supermart",
        "fieldName": "merchantName",
        "regionX": 0.03, "regionY": 0.01, "regionW": 0.15, "regionH": 0.02,
        "sampleCount": 6,
        "lastUpdated": "2026-09-01T00:00:00Z",
    }

    with (
        patch("src.ocr_worker.pytesseract.image_to_data", return_value=full_tess_data),
        patch.object(worker, "_fetch_templates", return_value={"merchantName": stored_template}),
        patch.object(worker, "_run_targeted_pass", return_value=("SUPERMAR7", 40)),
        patch.object(worker, "_scan_barcode", return_value=(None, None)),
    ):
        result = worker._run_pipeline("receipt-template-test-2", str(receipt_path))

    assert result["merchantName"] == "SUPERMART"
    assert result["confidence"]["merchantName"] == 92  # noqa: PLR2004


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
