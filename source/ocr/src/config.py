from __future__ import annotations

from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    redis_url: str = "redis://localhost:6379"
    api_base_url: str = "http://localhost:5000"
    internal_api_key: str = ""
    storage_receipts_path: str = "/storage/receipts"
    storage_thumbnails_path: str = "/storage/thumbnails"
    storage_ocr_json_path: str = "/storage/ocr-json"
    thumbnail_max_width: int = 300
    thumbnail_max_height: int = 400
    # OCR pipeline target in seconds (NFR: < 8s)
    ocr_timeout_seconds: int = 8

    class Config:
        env_file = ".env"
