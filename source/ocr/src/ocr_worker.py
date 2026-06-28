"""
OCR extraction worker.

Reads jobs from the Redis stream ``ocr.jobs`` (consumer group ``ocr-workers``),
runs the full OCR pipeline, and publishes structured results to ``ocr.results``.

Pipeline stages:
  1. Load image (JPG/PNG/HEIC/PDF)
  2. Preprocess with OpenCV (grayscale → deskew → denoise → adaptive threshold)
  3. Extract text with Tesseract
  4. Parse structured fields (merchant, date, total, subtotal, tax, line items)
  5. Scan barcodes with pyzbar
  6. Write raw OCR JSON to /storage/ocr-json/<receiptId>.json
  7. Publish result to ocr.results stream

Target: full pipeline < 8 seconds (configurable via Settings.ocr_timeout_seconds).
"""

from __future__ import annotations

import asyncio
import json
import logging
import re
import time
from datetime import datetime
from pathlib import Path
from typing import Any

import cv2
import numpy as np
import pytesseract
import redis.asyncio as aioredis
from redis.exceptions import TimeoutError as RedisTimeoutError
from PIL import Image
from pytesseract import Output

from .config import Settings

logger = logging.getLogger(__name__)

# ── Redis stream / consumer group names ────────────────────────────────────────
_JOBS_STREAM = "ocr.jobs"
_RESULTS_STREAM = "ocr.results"
_CONSUMER_GROUP = "ocr-workers"
_CONSUMER_NAME = "ocr-worker-1"

# ── Tesseract config ────────────────────────────────────────────────────────────
# --oem 1 = LSTM engine only  --psm 6 = assume uniform block of text
_TESS_CONFIG = "--oem 1 --psm 6"

# ── Target DPI for preprocessing (scale to approx 300 DPI equivalent) ─────────
_TARGET_DPI_WIDTH = 2100  # ~8.5 inch * 300 DPI — minimum useful width

# ── Keyword patterns for field extraction ─────────────────────────────────────
_TOTAL_PATTERN = re.compile(
    r"(?:grand\s+total|amount\s+due|total\s+due|total)[^\d]*(\d[\d,]*\.?\d*)",
    re.IGNORECASE,
)
_SUBTOTAL_PATTERN = re.compile(
    r"(?:sub\s*total|subtotal)[^\d]*(\d[\d,]*\.?\d*)",
    re.IGNORECASE,
)
_TAX_PATTERN = re.compile(
    r"(?:tax|gst|vat|hst)[^\d]*(\d[\d,]*\.?\d*)",
    re.IGNORECASE,
)

# Date formats: "Jan 15 2026", "15/01/2026", "01/15/2026", "2026-01-15"
_DATE_PATTERNS = [
    re.compile(r"\b(\d{4}-\d{2}-\d{2})\b"),
    re.compile(r"\b(\d{2}/\d{2}/\d{4})\b"),
    re.compile(r"\b(\w{3,9}\s+\d{1,2},?\s+\d{4})\b"),
]

# Time pattern: "14:32" or "2:32 PM"
_TIME_PATTERN = re.compile(r"\b(\d{1,2}:\d{2}(?:\s*[AaPp][Mm])?)\b")

# Line item: description followed by an optional quantity then a price at line end.
# Quantity is optional — most receipts only print description + price per line.
# Pattern: <description>  [<qty>  ]  <price>
_LINE_ITEM_PATTERN = re.compile(
    r"^(.+?)\s+(?:(\d+(?:\.\d+)?)\s+)?\$?(\d[\d,]*\.\d{2})\s*$"
)


class OcrWorker:
    """Consumes ocr.jobs stream entries and produces structured OCR results."""

    def __init__(self, settings: Settings) -> None:
        self._settings = settings
        self._redis = aioredis.from_url(settings.redis_url, decode_responses=True)
        pytesseract.pytesseract.tesseract_cmd = settings.tesseract_cmd

    async def run(self) -> None:
        """Main loop — reads from the Redis stream and processes jobs."""
        await self._ensure_consumer_group()
        logger.info(
            "OCR worker listening on stream '%s' group '%s'",
            _JOBS_STREAM,
            _CONSUMER_GROUP,
        )
        while True:
            try:
                entries = await self._redis.xreadgroup(
                    groupname=_CONSUMER_GROUP,
                    consumername=_CONSUMER_NAME,
                    streams={_JOBS_STREAM: ">"},
                    count=5,
                    block=5000,  # 5 second block wait
                )
                if not entries:
                    continue
                for _stream, messages in entries:
                    for message_id, fields in messages:
                        await self._process(message_id, fields)
            except RedisTimeoutError:
                continue  # xreadgroup block expired — no jobs, poll again
            except Exception:
                logger.exception("Unhandled error in OCR worker loop")
                await asyncio.sleep(1)

    async def _ensure_consumer_group(self) -> None:
        try:
            await self._redis.xgroup_create(
                _JOBS_STREAM, _CONSUMER_GROUP, id="$", mkstream=True
            )
        except Exception as exc:
            if "BUSYGROUP" in str(exc):
                logger.debug("Consumer group '%s' already exists", _CONSUMER_GROUP)
            else:
                raise

    async def _process(self, message_id: str, fields: dict[str, str]) -> None:
        """Process a single job message with up to 3 attempts (exponential backoff).

        Retry delays: 10s after attempt 1, 30s after attempt 2, then fail.
        Always ACKs the Redis message regardless of outcome to prevent infinite requeue.
        """
        _RETRY_DELAYS = [10, 30]  # seconds between attempts 1→2 and 2→3
        _MAX_ATTEMPTS = 3

        payload_str = fields.get("payload", "")
        receipt_id: str | None = None
        try:
            payload = json.loads(payload_str)
            receipt_id = payload["receiptId"]
            file_path = payload["filePath"]

            last_error: str = ""
            for attempt in range(1, _MAX_ATTEMPTS + 1):
                try:
                    if attempt > 1:
                        delay = _RETRY_DELAYS[attempt - 2]
                        logger.warning(
                            "Retrying OCR for receipt %s (attempt %d/%d) after %ds",
                            receipt_id, attempt, _MAX_ATTEMPTS, delay,
                        )
                        # Publish retry status so the UI can show "Processing (retry X of 3)"
                        await self._publish_retry_status(receipt_id, attempt, _MAX_ATTEMPTS)
                        await asyncio.sleep(delay)

                    logger.info(
                        "Starting OCR pipeline for receipt %s (attempt %d/%d)",
                        receipt_id, attempt, _MAX_ATTEMPTS,
                    )
                    t_start = time.perf_counter()

                    result = await asyncio.to_thread(
                        self._run_pipeline, receipt_id, file_path
                    )

                    elapsed = time.perf_counter() - t_start
                    logger.info(
                        "OCR pipeline completed for receipt %s in %.2fs (attempt %d)",
                        receipt_id, elapsed, attempt,
                    )
                    await self._publish_result(receipt_id, result)
                    return  # success — stop retrying

                except Exception as exc:
                    last_error = str(exc)
                    logger.warning(
                        "OCR attempt %d/%d failed for receipt %s: %s",
                        attempt, _MAX_ATTEMPTS, receipt_id, last_error,
                    )

            # All attempts exhausted.
            logger.error(
                "OCR failed after %d attempts for receipt %s: %s",
                _MAX_ATTEMPTS, receipt_id, last_error,
            )
            await self._publish_error(receipt_id, last_error)

        except Exception as exc:
            logger.exception("Unhandled error processing OCR job message %s", message_id)
            if receipt_id:
                await self._publish_error(receipt_id, str(exc))
        finally:
            await self._redis.xack(_JOBS_STREAM, _CONSUMER_GROUP, message_id)

    # ── Pipeline ────────────────────────────────────────────────────────────────

    def _run_pipeline(
        self, receipt_id: str, file_path: str
    ) -> dict[str, Any]:
        """
        Synchronous OCR pipeline (runs in a thread pool via asyncio.to_thread).

        Returns a dict matching the ocr.results message schema.
        """
        src = Path(file_path)

        # 1. Load original image (for barcode scanning before preprocessing).
        original_pil = self._load_image(src)
        original_cv = cv2.cvtColor(np.array(original_pil), cv2.COLOR_RGB2BGR)

        # 2. Scan barcode/QR on original (unprocessed) image.
        barcode = self._scan_barcode(original_cv)  # returns (data, type) tuple

        # 3. Preprocess.
        preprocessed = self._preprocess(original_cv)

        # 4. Tesseract extraction.
        tess_data = pytesseract.image_to_data(
            preprocessed,
            config=_TESS_CONFIG,
            output_type=Output.DICT,
        )

        full_text = " ".join(
            w for w in tess_data["text"] if w.strip()
        )
        lines = self._build_lines(tess_data)

        # 5. Parse structured fields.
        merchant_name, merchant_conf = self._extract_merchant(tess_data, lines)
        date_str, date_conf = self._extract_date(full_text)
        time_str = self._extract_time(full_text)
        total, total_conf = self._extract_amount(_TOTAL_PATTERN, full_text)
        subtotal, _ = self._extract_amount(_SUBTOTAL_PATTERN, full_text)
        tax_amount, _ = self._extract_amount(_TAX_PATTERN, full_text)
        line_items = self._extract_line_items(lines)

        # 6. Write raw OCR JSON.
        raw_ocr_path = self._write_raw_ocr(receipt_id, tess_data, full_text)

        barcode_data, barcode_type = barcode

        return {
            "receiptId": receipt_id,
            "status": "complete",
            "merchantName": merchant_name,
            "merchantAddress": None,  # Address heuristics are Sprint 3 ML work.
            "date": date_str,
            "time": time_str,
            "subtotal": subtotal,
            "taxAmount": tax_amount,
            "total": total,
            "lineItems": line_items,
            "barcode": barcode_data,
            "barcodeType": barcode_type,
            "imageQuality": self._compute_image_quality(original_cv),
            "confidence": {
                "merchantName": merchant_conf,
                "date": date_conf,
                "total": total_conf,
            },
            "rawOcrPath": raw_ocr_path,
            "errorMessage": None,
        }

    # ── Image loading ────────────────────────────────────────────────────────────

    def _load_image(self, src: Path) -> Image.Image:
        """Load image from disk, handling HEIC and PDF sources."""
        suffix = src.suffix.lower()

        if suffix in (".heic", ".heif"):
            import pillow_heif  # type: ignore[import-untyped]

            pillow_heif.register_heif_opener()

        if suffix == ".pdf":
            from pdf2image import convert_from_path  # type: ignore[import-untyped]

            pages = convert_from_path(str(src), first_page=1, last_page=1, dpi=200)
            if not pages:
                raise ValueError(f"Could not render PDF first page: {src}")
            return pages[0]

        return Image.open(src).convert("RGB")

    # ── Preprocessing ────────────────────────────────────────────────────────────

    def _preprocess(self, img: np.ndarray) -> np.ndarray:  # type: ignore[type-arg]
        """
        Full preprocessing pipeline:
          grayscale → scale to ≥2100px wide → deskew → denoise → adaptive threshold.
        """
        # Grayscale.
        gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)

        # Scale up if narrower than target (improves Tesseract accuracy).
        h, w = gray.shape
        if w < _TARGET_DPI_WIDTH:
            scale = _TARGET_DPI_WIDTH / w
            gray = cv2.resize(
                gray,
                (int(w * scale), int(h * scale)),
                interpolation=cv2.INTER_CUBIC,
            )

        # Deskew.
        gray = self._deskew(gray)

        # Denoise.
        gray = cv2.fastNlMeansDenoising(gray, h=10, templateWindowSize=7, searchWindowSize=21)

        # Adaptive threshold — binarise for Tesseract.
        gray = cv2.adaptiveThreshold(
            gray,
            255,
            cv2.ADAPTIVE_THRESH_GAUSSIAN_C,
            cv2.THRESH_BINARY,
            blockSize=31,
            C=10,
        )

        return gray

    def _deskew(self, gray: np.ndarray) -> np.ndarray:  # type: ignore[type-arg]
        """
        Estimate skew angle via Hough lines and rotate to correct.
        Falls back to original image if no lines are detected.
        """
        edges = cv2.Canny(gray, 50, 150, apertureSize=3)
        lines = cv2.HoughLinesP(
            edges, 1, np.pi / 180, threshold=100, minLineLength=100, maxLineGap=10
        )

        if lines is None:
            return gray

        angles: list[float] = []
        for line in lines:
            x1, y1, x2, y2 = line[0]
            if x2 != x1:
                angles.append(float(np.degrees(np.arctan2(y2 - y1, x2 - x1))))

        if not angles:
            return gray

        median_angle = float(np.median(angles))
        # Only correct small skew (< 10 degrees) to avoid over-rotation.
        if abs(median_angle) > 10:  # noqa: PLR2004
            return gray

        h, w = gray.shape
        centre = (w // 2, h // 2)
        rotation_matrix = cv2.getRotationMatrix2D(centre, median_angle, 1.0)
        rotated = cv2.warpAffine(
            gray,
            rotation_matrix,
            (w, h),
            flags=cv2.INTER_CUBIC,
            borderMode=cv2.BORDER_REPLICATE,
        )
        return rotated

    # ── Field extraction ─────────────────────────────────────────────────────────

    def _build_lines(self, tess_data: dict[str, list[Any]]) -> list[str]:
        """Group Tesseract words by line number to reconstruct text lines."""
        lines: dict[int, list[str]] = {}
        for i, word in enumerate(tess_data["text"]):
            if not word.strip():
                continue
            line_num: int = tess_data["line_num"][i]
            lines.setdefault(line_num, []).append(word)
        return [" ".join(words) for words in lines.values()]

    def _extract_merchant(
        self, tess_data: dict[str, list[Any]], lines: list[str]
    ) -> tuple[str | None, int]:
        """
        Merchant name heuristic: largest-font text in the top 15% of the image.
        Falls back to the first non-empty line if no large-font text is found.
        """
        img_height = max(tess_data["top"]) + 1 if tess_data["top"] else 1
        top_threshold = img_height * 0.15

        # Find words in the top 15% with a positive confidence score.
        candidate_words: list[tuple[int, str, int]] = []
        for i, word in enumerate(tess_data["text"]):
            if not word.strip():
                continue
            conf: int = int(tess_data["conf"][i])
            top: int = tess_data["top"][i]
            height: int = tess_data["height"][i]
            if top <= top_threshold and conf > 0:
                candidate_words.append((height, word, conf))

        if not candidate_words:
            # Fall back to first non-empty line.
            if lines:
                return lines[0][:80], 40
            return None, 0

        # Use the tallest (largest font) words as merchant name.
        candidate_words.sort(key=lambda t: t[0], reverse=True)
        max_height = candidate_words[0][0]
        merchant_words = [w for h, w, _ in candidate_words if h >= max_height * 0.8]
        confs = [c for h, _, c in candidate_words if h >= max_height * 0.8]
        avg_conf = int(sum(confs) / len(confs)) if confs else 0

        return " ".join(merchant_words[:6])[:80], avg_conf

    def _extract_date(self, text: str) -> tuple[str | None, int]:
        """
        Scan text for common date formats and return ISO 8601 or None.
        Confidence is 90 if parsed successfully, 0 otherwise.
        """
        for pattern in _DATE_PATTERNS:
            match = pattern.search(text)
            if match:
                raw = match.group(1)
                iso = self._parse_date_to_iso(raw)
                if iso:
                    return iso, 90
        return None, 0

    def _parse_date_to_iso(self, raw: str) -> str | None:
        """Try several date formats and return ISO 8601 string or None."""
        formats = [
            "%Y-%m-%d",
            "%d/%m/%Y",
            "%m/%d/%Y",
            "%B %d %Y",
            "%B %d, %Y",
            "%b %d %Y",
            "%b %d, %Y",
        ]
        for fmt in formats:
            try:
                dt = datetime.strptime(raw.strip(), fmt)
                return dt.strftime("%Y-%m-%dT00:00:00Z")
            except ValueError:
                continue
        return None

    def _extract_time(self, text: str) -> str | None:
        match = _TIME_PATTERN.search(text)
        return match.group(1) if match else None

    def _extract_amount(
        self, pattern: re.Pattern[str], text: str
    ) -> tuple[float | None, int]:
        """
        Match a currency-amount pattern. Returns (float, confidence) or (None, 0).
        Confidence is 85 if found, 0 otherwise.
        """
        match = pattern.search(text)
        if not match:
            return None, 0
        try:
            value = float(match.group(1).replace(",", ""))
            return value, 85
        except ValueError:
            return None, 0

    def _extract_line_items(
        self, lines: list[str]
    ) -> list[dict[str, Any]]:
        """
        Parse line items from the receipt lines.
        Pattern: description [quantity] price
        Quantity defaults to 1 when not present on the line.
        """
        items: list[dict[str, Any]] = []
        for line in lines:
            m = _LINE_ITEM_PATTERN.match(line.strip())
            if m:
                name = m.group(1)
                qty_str = m.group(2)   # None when quantity column absent
                price_str = m.group(3)
                try:
                    items.append({
                        "name": name.strip()[:120],
                        "quantity": float(qty_str) if qty_str else 1.0,
                        "unitPrice": float(price_str.replace(",", "")),
                    })
                except ValueError:
                    continue
        return items

    # ── Barcode ──────────────────────────────────────────────────────────────────

    def _scan_barcode(
        self, img: np.ndarray  # type: ignore[type-arg]
    ) -> tuple[str | None, str | None]:
        """Scan the original image for barcodes and QR codes using pyzbar.

        Returns:
            A tuple of (decoded_data, barcode_type) where barcode_type is the
            pyzbar symbol type string (e.g. "QRCODE", "CODE128", "EAN13").
            Both values are None when no code is found or pyzbar is unavailable.
        """
        try:
            from pyzbar.pyzbar import decode  # type: ignore[import-untyped]

            decoded = decode(img)
            if decoded:
                symbol = decoded[0]
                data = symbol.data.decode("utf-8", errors="replace")
                barcode_type = symbol.type  # e.g. "QRCODE", "CODE128", "EAN13"
                return data, barcode_type
        except ImportError:
            logger.warning("pyzbar not installed — barcode scanning skipped")
        except Exception:
            logger.exception("Barcode scan failed — continuing without barcode data")
        return None, None

    def _compute_image_quality(self, img: np.ndarray) -> str:  # type: ignore[type-arg]
        """Estimate image quality using Laplacian variance (blur) and RMS contrast.

        Returns:
            "low" when the image is likely too blurry or low-contrast for good OCR;
            "good" otherwise.
        """
        gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)

        # Blur score: variance of the Laplacian — low value means blurry.
        blur_score = float(cv2.Laplacian(gray, cv2.CV_64F).var())

        # Contrast: RMS of pixel intensity deviations from mean.
        mean, std_dev = cv2.meanStdDev(gray)
        contrast_score = float(std_dev[0][0])

        _BLUR_THRESHOLD = 80.0
        _CONTRAST_THRESHOLD = 30.0

        if blur_score < _BLUR_THRESHOLD or contrast_score < _CONTRAST_THRESHOLD:
            logger.debug(
                "Low image quality detected: blur=%.1f contrast=%.1f",
                blur_score, contrast_score,
            )
            return "low"
        return "good"

    # ── Output ───────────────────────────────────────────────────────────────────

    def _write_raw_ocr(
        self,
        receipt_id: str,
        tess_data: dict[str, list[Any]],
        full_text: str,
    ) -> str:
        """Persist raw Tesseract output to /storage/ocr-json/<receiptId>.json."""
        ocr_dir = Path(self._settings.storage_ocr_json_path)
        ocr_dir.mkdir(parents=True, exist_ok=True)
        dest = ocr_dir / f"{receipt_id}.json"

        # Convert numpy int types to native Python for JSON serialisation.
        serialisable: dict[str, Any] = {
            k: [int(v) if hasattr(v, "item") else v for v in vals]
            for k, vals in tess_data.items()
        }
        serialisable["fullText"] = full_text

        dest.write_text(json.dumps(serialisable, ensure_ascii=False, indent=2))
        return str(dest)

    async def _publish_result(
        self, receipt_id: str, result: dict[str, Any]
    ) -> None:
        """Publish structured OCR result to the ocr.results Redis stream."""
        await self._redis.xadd(
            _RESULTS_STREAM,
            {"payload": json.dumps(result)},
        )
        logger.debug("Published OCR result for receipt %s to %s", receipt_id, _RESULTS_STREAM)

    async def _publish_retry_status(
        self, receipt_id: str, attempt: int, max_attempts: int
    ) -> None:
        """Publish a retry-in-progress status so the API can surface it to the UI."""
        payload = {
            "receiptId": receipt_id,
            "status": f"processing (retry {attempt} of {max_attempts})",
            "attempt": attempt,
            "maxAttempts": max_attempts,
        }
        await self._redis.xadd(
            _RESULTS_STREAM,
            {"payload": json.dumps(payload)},
        )

    async def _publish_error(self, receipt_id: str, error_message: str) -> None:
        """Publish an ocr_failed result when all retry attempts are exhausted."""
        result = {
            "receiptId": receipt_id,
            "status": "ocr_failed",
            "merchantName": None,
            "merchantAddress": None,
            "date": None,
            "time": None,
            "subtotal": None,
            "taxAmount": None,
            "total": None,
            "lineItems": [],
            "barcode": None,
            "barcodeType": None,
            "imageQuality": None,
            "confidence": {},
            "rawOcrPath": None,
            "errorMessage": error_message,
        }
        await self._redis.xadd(
            _RESULTS_STREAM,
            {"payload": json.dumps(result)},
        )
