# MEMORY.md — Architecture & Decision Log

---

## 2026-07-05, Sprint 9 — scoped US-INT-05 template learning to merchant-name only

**What was decided:**
- Implemented Sprint 9 (recurring expense detection, merchant aliases, intelligence settings page, merchant field-templates) using `/senior-backend`, `/senior-frontend`, `/senior-ml-engineer`.
- For US-INT-05 (merchant receipt layout templates), scoped the OCR-worker template-guided extraction and region-learning to the `merchantName` field only, instead of all three fields (`merchantName`, `total`, `date`) called for in the sprint plan.
- Also deliberately did not implement the monthly "recurring expense missing" notification alert from US-INT-06, though the underlying `recurring_expenses` data model and nightly detection job are complete.

**Why:**
- The existing OCR pipeline (`source/ocr/src/ocr_worker.py`) only tracks per-word bounding boxes for the merchant-name heuristic (largest-font-in-top-15%). `total` and `date` are extracted via regex over concatenated full-text with no word-index tracking back to Tesseract's per-word position data.
- Fabricating approximate bounding boxes for `total`/`date` under time pressure would have produced fragile, likely-wrong regions that silently degrade template quality once enough samples accumulate — worse than not building the feature. Shipping a correct, narrower slice (merchant-name templates working end-to-end: fetch, targeted crop, confidence comparison, logging, region-learning on confirmation) was chosen over a "complete-looking" but subtly broken three-field version.
- The recurring-expense notification alert was skipped for the same reason: it depends on `documents/sprint-plan.md`'s roles (`Owner`/`AdultMember`) that don't exist in this codebase's actual `Admin`/`Contributor`/`Reader` model, and needed more design time than remained in this session to map onto the existing `NotificationEntity`/`Notification` module correctly.

**What was rejected:**
- Building fake/approximate bounding boxes for `total`/`date` by guessing word positions from regex match offsets: rejected as too unreliable to trust for a weighted-moving-average template store that gets more confident (and thus more consulted) over time.
- Skipping tests entirely for the OCR changes because no Python interpreter was available in this session to run them: rejected — wrote them anyway (mirroring `test_ocr_worker.py` conventions, mocking Tesseract/httpx) and flagged clearly that they needed a real `pytest` run before merging, rather than shipping untested code silently.

**Follow-up (same day):** the user ran `pytest` against `source/ocr/.venv` and got 34 collection errors — not from anything in this session's changes, but from a pre-existing `Settings` fixture bug (`storage_receipts_path` etc. are read-only `@property` values, not constructor fields) that meant the *entire* OCR test suite, including files untouched this session, had never actually passed. Fixed that plus 5 further pre-existing bugs it uncovered (a real receipt-parsing bug in `_extract_line_items`, a retry-test with a 40-second real sleep and a wrong assertion, two thumbnail tests opening a relative path directly, one stale mock assertion). Final state: 58 passed, 0 failed, `ruff check` clean on touched files. Full detail in `documents/sprint-plan.md` Sprint 9 Review Notes.

---

## 2026-07-05, Sprint 8 gap-fixing before Sprint 9 kickoff

**What was decided:**
- Before starting Sprint 9, ran a code-level audit of Sprint 8 (commit `ff39f9b`) since it had been marked complete without verification. Found the category-suggestion feature was dead code (service method never called), the OCR-accuracy feedback loop always computed 0%, duplicate-dismissal was keyed to the wrong expense ID, and the shared merchant-normalization fixture had 5 incorrect entries that no test ever loaded.
- Fixed all of the above, added real test coverage (`IntelligenceEndpointsTests.cs`, `IntelligenceServiceTests.cs`, `MerchantNormalizerFixtureTests.cs`, `test_merchant_normalizer.py`), and along the way fixed two pre-existing EF Core test-infrastructure bugs (`IDbContextOptionsConfiguration<T>` not removed; unconditional `MigrateAsync()` failing against the in-memory provider) that were blocking every integration test in the repo, not just Sprint 8's.
- Chose NOT to chase down a third, deeper bug discovered in the process: login-based HTTP integration tests return 401 for freshly-seeded users across all sprints (6/7/8). Full details in `documents/sprint-plan.md` under "Sprint 8 — Review Notes (2026-07-05)".

**Why:**
- Sprint 9 (US-INT-05, merchant template learning) builds directly on Sprint 8's correction-history feedback loop — starting Sprint 9 on a silently-broken foundation would have compounded the bug into new work.
- Full DoD checklists had been checked off without running anything; "build succeeds" was being conflated with "feature works." Fixing this now is cheaper than discovering it mid-Sprint-9.
- The login-401 issue is a separate, deeper pre-existing problem not unique to Sprint 8 and not blocking Sprint 9's ability to proceed at the code level — chasing it further was scope creep beyond the requested gap-fixing pass.

**What was rejected:**
- Logging the Sprint 8 gaps as tech debt and proceeding straight to Sprint 9: rejected because Sprint 9's core story explicitly depends on the correction-history data Sprint 8 was supposed to produce correctly.
- Debugging the login-401 test-infra issue to completion in this same pass: rejected as out of scope — it predates Sprint 8, affects the whole test suite, and isn't a blocker for writing Sprint 9 code (unit/fixture-level tests already validate the Sprint 8 logic fixes independently of the HTTP layer).

---

## 2026-06-07, US-AUTH-02 MFA Implementation

**What was decided:**
- TOTP secret is NOT persisted during `POST /auth/mfa/setup`. It is only stored after `POST /auth/mfa/enable` confirms a valid OTP from the user's authenticator app.
- AES-256-CBC is used to encrypt the TOTP secret at rest, with a key from `Mfa:EncryptionKey` (64-char hex) in app configuration.
- The MFA login flow is session-based (not token-based): after password validation, a `MfaPending` session key holds the user ID until `POST /auth/mfa/login` completes.
- Admin MFA toggle (`PATCH /admin/users/{id}/mfa`) sets `mfa_enabled` flag only; the user must complete setup themselves when admin enables MFA.
- Endpoint rename: `/auth/mfa/verify` split into `/auth/mfa/enable` (setup confirm) and `/auth/mfa/login` (login challenge). Old `VerifyMfaAsync` on `IAuthService` removed and replaced with `EnableMfaAsync` + `VerifyMfaLoginAsync`.

**Why:**
- Separating setup confirmation from login verification prevents a race condition where a partially set-up MFA blocks login.
- AES-256-CBC with per-encryption random IV ensures that even identical secrets produce distinct ciphertexts (prevents correlation attacks against the DB).
- Session-based pending state avoids storing short-lived tokens in Redis, keeping the architecture simple and consistent with the existing session-auth pattern.

**What was rejected:**
- Redis-based `pendingToken` approach (as originally spec'd): rejected because the project already has session infrastructure and it would add complexity for no privacy benefit in a self-hosted deployment.
- Persisting the TOTP secret immediately during setup: rejected because the user may never complete setup (scans QR but never verifies), polluting the DB with unvalidated secrets.
- JWT short-lived token for MFA pending: rejected to keep auth uniformly session-based (no Bearer tokens in localStorage per CLAUDE.md).

---
