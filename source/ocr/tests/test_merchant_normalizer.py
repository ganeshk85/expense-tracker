"""
Tests for merchant name normalization.

Loads the shared fixture file also used by the .NET MerchantNormalizer tests
to verify both implementations produce identical output for the same input.
"""

from __future__ import annotations

import json
from pathlib import Path

import pytest

from src.merchant_normalizer import normalize_merchant

_FIXTURES_PATH = Path(__file__).parent / "merchant_normalization_fixtures.json"


def _load_fixtures() -> list[dict[str, str]]:
    with _FIXTURES_PATH.open(encoding="utf-8") as f:
        return json.load(f)


@pytest.mark.parametrize("case", _load_fixtures(), ids=lambda c: repr(c["input"]))
def test_normalize_merchant_matches_shared_fixture(case: dict[str, str]) -> None:
    assert normalize_merchant(case["input"]) == case["expected"]
