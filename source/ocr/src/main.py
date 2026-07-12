from __future__ import annotations

import asyncio
import logging

import redis.asyncio as aioredis
from fastapi import FastAPI

from .config import Settings
from .correction_consumer import CorrectionConsumer
from .ocr_worker import OcrWorker
from .thumbnail_worker import ThumbnailWorker

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(title="Expense Tracker OCR Worker")
settings = Settings()


@app.on_event("startup")
async def start_workers() -> None:
    thumbnail_worker = ThumbnailWorker(settings)
    asyncio.create_task(thumbnail_worker.run())
    logger.info("Thumbnail worker started")

    ocr_worker = OcrWorker(settings)
    asyncio.create_task(ocr_worker.run())
    logger.info("OCR worker started")

    redis_client = aioredis.from_url(settings.redis_url, decode_responses=False)
    correction_consumer = CorrectionConsumer(redis_client, settings)
    asyncio.create_task(correction_consumer.start())
    logger.info("Correction consumer started")


@app.get("/health")
async def health() -> dict[str, str]:
    return {"status": "ok"}
