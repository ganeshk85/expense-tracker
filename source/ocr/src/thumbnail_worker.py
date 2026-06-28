from __future__ import annotations

import asyncio
import json
import logging
import os
from pathlib import Path

import httpx
import redis.asyncio as aioredis
from redis.exceptions import TimeoutError as RedisTimeoutError
from PIL import Image

from .config import Settings

logger = logging.getLogger(__name__)

_REDIS_QUEUE = "receipt.uploaded"
_POLL_INTERVAL = 0.5


class ThumbnailWorker:
    """Consumes receipt upload events and generates thumbnails."""

    def __init__(self, settings: Settings) -> None:
        self._settings = settings
        self._redis = aioredis.from_url(settings.redis_url, decode_responses=True)

    async def run(self) -> None:
        logger.info("Thumbnail worker listening on queue '%s'", _REDIS_QUEUE)
        while True:
            try:
                item = await self._redis.brpop(_REDIS_QUEUE, timeout=5)
                if item is None:
                    continue
                _, payload = item
                await self._process(json.loads(payload))
            except RedisTimeoutError:
                continue  # brpop block expired — queue empty, poll again
            except Exception:
                logger.exception("Unhandled error in thumbnail worker loop")
                await asyncio.sleep(_POLL_INTERVAL)

    async def _process(self, job: dict[str, str]) -> None:
        receipt_id: str = job["receiptId"]
        file_path: str = job["filePath"]

        logger.info("Generating thumbnail for receipt %s", receipt_id)

        try:
            thumbnail_path = await asyncio.to_thread(
                self._generate_thumbnail, receipt_id, file_path
            )
            await self._notify_api(receipt_id, thumbnail_path)
            logger.info("Thumbnail ready for receipt %s at %s", receipt_id, thumbnail_path)
        except Exception:
            logger.exception("Failed to generate thumbnail for receipt %s", receipt_id)

    def _generate_thumbnail(self, receipt_id: str, file_path: str) -> str:
        """Generate thumbnail synchronously (CPU-bound, run in thread pool).

        Returns a relative path (thumbnails/{receipt_id}.jpg) so the API can store
        it portably without coupling the DB to this machine's absolute paths.
        """
        src = Path(file_path)
        thumbnails_dir = Path(self._settings.storage_thumbnails_path)
        thumbnails_dir.mkdir(parents=True, exist_ok=True)

        dest = thumbnails_dir / f"{receipt_id}.jpg"

        image = self._load_image(src)
        image.thumbnail(
            (self._settings.thumbnail_max_width, self._settings.thumbnail_max_height),
            Image.LANCZOS,
        )

        if image.mode in ("RGBA", "P"):
            image = image.convert("RGB")

        image.save(dest, format="JPEG", quality=85, optimize=True)
        return f"thumbnails/{receipt_id}.jpg"

    def _load_image(self, src: Path) -> Image.Image:
        suffix = src.suffix.lower()

        if suffix in (".heic", ".heif"):
            import pillow_heif
            pillow_heif.register_heif_opener()

        if suffix == ".pdf":
            from pdf2image import convert_from_path
            pages = convert_from_path(str(src), first_page=1, last_page=1, dpi=150)
            if not pages:
                raise ValueError(f"Could not render PDF: {src}")
            return pages[0]

        return Image.open(src)

    async def _notify_api(self, receipt_id: str, thumbnail_path: str) -> None:
        """Tell the API the thumbnail is ready via internal PATCH endpoint."""
        url = f"{self._settings.api_base_url}/receipts/{receipt_id}/thumbnail"
        headers = {"X-Internal-Key": self._settings.internal_api_key}
        async with httpx.AsyncClient(timeout=10.0) as client:
            resp = await client.patch(url, json={"thumbnailPath": thumbnail_path}, headers=headers)
            resp.raise_for_status()
