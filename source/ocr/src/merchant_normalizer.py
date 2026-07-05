"""
Merchant name normalization for the expense-tracker OCR worker.

Algorithm must produce IDENTICAL output to the .NET MerchantNormalizer.cs
in ExpenseTracker.Shared — verified by shared test fixtures at
tests/merchant_normalization_fixtures.json.

Rules:
  1. Lowercase
  2. Remove punctuation characters: . , ' " - _ & /
  3. Collapse whitespace to single space
  4. Strip leading/trailing whitespace
"""

import re

_PUNCTUATION_RE = re.compile(r"[.,'\"\-_&/]")
_WHITESPACE_RE = re.compile(r"\s+")


def normalize_merchant(name: str | None) -> str:
    """Return the canonical normalized form of a merchant name.

    Returns an empty string for None or whitespace-only input.
    """
    if not name or not name.strip():
        return ""

    result = name.lower()
    result = _PUNCTUATION_RE.sub(" ", result)
    result = _WHITESPACE_RE.sub(" ", result)
    return result.strip()
