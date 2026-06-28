from __future__ import annotations

from pathlib import Path

from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    redis_url: str = "redis://localhost:6379"
    api_base_url: str = "http://localhost:5000"
    internal_api_key: str = ""
    # Single base path — sub-directories are derived, matching StorageOptions on the .NET side.
    storage_base_path: str = "/storage"
    thumbnail_max_width: int = 300
    thumbnail_max_height: int = 400
    # Full path to the Tesseract executable (required on Windows where it is not on PATH)
    tesseract_cmd: str = "tesseract"
    # OCR pipeline target in seconds (NFR: < 8s)
    ocr_timeout_seconds: int = 8

    @property
    def storage_receipts_path(self) -> str:
        return str(Path(self.storage_base_path) / "receipts")

    @property
    def storage_thumbnails_path(self) -> str:
        return str(Path(self.storage_base_path) / "thumbnails")

    @property
    def storage_ocr_json_path(self) -> str:
        return str(Path(self.storage_base_path) / "ocr-json")

    class Config:
        env_file = ".env"
