"""
Correction consumer for the OCR accuracy feedback loop.

Reads from the ocr.correction Redis stream, maintains an in-memory
accuracy dict keyed by (merchant_normalized, field), and adaptively
adjusts Tesseract preprocessing configuration when a merchant's accuracy
drops below ACCURACY_THRESHOLD for any field.
"""

import asyncio
import json
import logging
from collections import defaultdict
from typing import Any

import redis.asyncio as aioredis

logger = logging.getLogger(__name__)

_STREAM = "ocr.correction"
_GROUP = "ocr-accuracy-py-workers"
_CONSUMER = "correction-consumer-1"
_POLL_INTERVAL_S = 0.5
ACCURACY_THRESHOLD = 0.70  # 70 % — trigger adaptive preprocessing below this


class CorrectionConsumer:
    """Redis stream consumer that tracks OCR field accuracy in memory.

    The .NET OcrCorrectionConsumerService persists the data to the DB;
    this Python consumer drives the adaptive preprocessing feedback loop
    so the OCR worker can tune its pipeline without a DB round-trip.
    """

    def __init__(self, redis_client: aioredis.Redis) -> None:
        self._redis = redis_client
        # {(merchant_normalized, field): {"extractions": int, "corrections": int}}
        self._accuracy: dict[tuple[str, str], dict[str, int]] = defaultdict(
            lambda: {"extractions": 0, "corrections": 0}
        )
        # Merchants flagged for adaptive preprocessing
        self._adaptive_merchants: set[str] = set()
        self._running = False

    async def start(self) -> None:
        """Create consumer group if missing, then start reading loop."""
        try:
            await self._redis.xgroup_create(
                _STREAM, _GROUP, id="$", mkstream=True
            )
        except aioredis.ResponseError as exc:
            if "BUSYGROUP" not in str(exc):
                raise
            logger.debug("Consumer group %s already exists", _GROUP)

        self._running = True
        logger.info("CorrectionConsumer started on stream '%s'", _STREAM)
        await self._loop()

    async def stop(self) -> None:
        self._running = False

    def should_use_adaptive_preprocessing(self, merchant_normalized: str) -> bool:
        """Return True if any field accuracy for this merchant is below threshold."""
        return merchant_normalized in self._adaptive_merchants

    def get_accuracy(self, merchant_normalized: str, field: str) -> float | None:
        """Return accuracy rate (0–1) or None if insufficient data (< 5 samples)."""
        key = (merchant_normalized, field)
        stats = self._accuracy.get(key)
        if stats is None or stats["extractions"] < 5:
            return None
        return 1.0 - (stats["corrections"] / stats["extractions"])

    # ── Private ───────────────────────────────────────────────────────────────

    async def _loop(self) -> None:
        while self._running:
            try:
                messages = await self._redis.xreadgroup(
                    _GROUP, _CONSUMER, {_STREAM: ">"}, count=20, block=500
                )
                if messages:
                    for _stream, entries in messages:
                        for msg_id, fields in entries:
                            await self._process(msg_id, fields)
                else:
                    await asyncio.sleep(_POLL_INTERVAL_S)
            except asyncio.CancelledError:
                break
            except Exception:
                logger.exception("Error in CorrectionConsumer loop")
                await asyncio.sleep(_POLL_INTERVAL_S)

    async def _process(self, msg_id: bytes, fields: dict[bytes, bytes]) -> None:
        try:
            payload_raw = fields.get(b"payload") or fields.get("payload")
            if not payload_raw:
                await self._redis.xack(_STREAM, _GROUP, msg_id)
                return

            payload: dict[str, Any] = json.loads(payload_raw)
            merchant = payload.get("merchantNormalized", "").strip()
            field = payload.get("field", "").strip()

            if merchant and field:
                key = (merchant, field)
                self._accuracy[key]["extractions"] += 1
                self._accuracy[key]["corrections"] += 1

                accuracy = self.get_accuracy(merchant, field)
                if accuracy is not None and accuracy < ACCURACY_THRESHOLD:
                    if merchant not in self._adaptive_merchants:
                        logger.warning(
                            "Accuracy for merchant=%s field=%s dropped to %.1f%% "
                            "— enabling adaptive preprocessing",
                            merchant,
                            field,
                            accuracy * 100,
                        )
                    self._adaptive_merchants.add(merchant)
                elif accuracy is not None and accuracy >= ACCURACY_THRESHOLD:
                    self._adaptive_merchants.discard(merchant)

        except Exception:
            logger.exception("Failed to process correction message %s", msg_id)
        finally:
            await self._redis.xack(_STREAM, _GROUP, msg_id)
