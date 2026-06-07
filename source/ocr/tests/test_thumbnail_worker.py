from __future__ import annotations

import io
from pathlib import Path
from unittest.mock import AsyncMock, MagicMock, patch

import pytest
from PIL import Image

from src.config import Settings
from src.thumbnail_worker import ThumbnailWorker


@pytest.fixture()
def settings(tmp_path: Path) -> Settings:
    return Settings(
        redis_url="redis://localhost:6379",
        api_base_url="http://localhost:5000",
        storage_receipts_path=str(tmp_path / "receipts"),
        storage_thumbnails_path=str(tmp_path / "thumbnails"),
    )


@pytest.fixture()
def worker(settings: Settings) -> ThumbnailWorker:
    return ThumbnailWorker(settings)


def make_jpeg(tmp_path: Path, name: str = "receipt.jpg") -> Path:
    p = tmp_path / name
    img = Image.new("RGB", (800, 1200), color=(200, 200, 200))
    img.save(p, format="JPEG")
    return p


def test_generate_thumbnail_creates_file(worker: ThumbnailWorker, tmp_path: Path) -> None:
    src = make_jpeg(tmp_path)
    (tmp_path / "thumbnails").mkdir(parents=True, exist_ok=True)

    receipt_id = "test-receipt-001"
    dest = worker._generate_thumbnail(receipt_id, str(src))

    assert Path(dest).exists()
    with Image.open(dest) as img:
        assert img.width <= 300
        assert img.height <= 400


def test_generate_thumbnail_converts_rgba(worker: ThumbnailWorker, tmp_path: Path) -> None:
    src = tmp_path / "receipt.png"
    img = Image.new("RGBA", (600, 800), color=(100, 150, 200, 128))
    img.save(src, format="PNG")
    (tmp_path / "thumbnails").mkdir(parents=True, exist_ok=True)

    dest = worker._generate_thumbnail("rgba-test", str(src))

    with Image.open(dest) as result:
        assert result.mode == "RGB"


@pytest.mark.asyncio()
async def test_notify_api_calls_patch(worker: ThumbnailWorker) -> None:
    with patch("src.thumbnail_worker.httpx.AsyncClient") as mock_client_cls:
        mock_client = AsyncMock()
        mock_client_cls.return_value.__aenter__.return_value = mock_client
        mock_resp = MagicMock()
        mock_resp.raise_for_status = MagicMock()
        mock_client.patch.return_value = mock_resp

        await worker._notify_api("receipt-123", "/storage/thumbnails/receipt-123.jpg")

        mock_client.patch.assert_called_once_with(
            "http://localhost:5000/receipts/receipt-123/thumbnail",
            json={"thumbnailPath": "/storage/thumbnails/receipt-123.jpg"},
        )
