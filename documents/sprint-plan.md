# Sprint Plan — Expense Tracker
**Project:** Family Expense Intelligence Platform
**Role:** Product Owner
**Date:** 2026-05-30
**Source:** documents/user-stories.md

---

## Team

| Engineer | Role | Domain |
|----------|------|--------|
| **Senior Backend Engineer (BE)** | Full-time | .NET 10 API, PostgreSQL, Redis, auth middleware |
| **Senior Frontend Engineer (FE)** | Full-time | React/Next.js, TypeScript, Tailwind CSS, React Query |
| **Senior OCR Engineer (OCR)** | Full-time | Python FastAPI, Tesseract, OpenCV, ZXing, file workers |

**Sprint cadence:** 2-week sprints
**Assumed velocity:** 18–22 pts committed (new project, conservative first sprints)
**Sprint 1 start:** 2026-06-02

---

## Prioritization Framework

Each story is scored across 4 dimensions (1–5 scale):

| Dimension | Weight | Question |
|-----------|--------|----------|
| Business Value | 40% | Strategic fit, mission impact, user demand |
| User Impact | 30% | How many users affected and how often |
| Risk / Dependencies | 15% | Blocks other stories? Technical uncertainty? |
| Effort (inverted) | 15% | Lower effort = higher score |

**Final Score = (Value × 0.40) + (Impact × 0.30) + (Risk × 0.15) + (Effort × 0.15)**

---

## Prioritized Backlog — Phase 1 (22 Stories)

### Tier 1 — Must Do First (gates and blockers)

| Story ID | Title | Points | Score | Reason |
|----------|-------|--------|-------|--------|
| US-AUTH-01 | User Login | 3 | 4.8 | Gates access to every other feature |
| US-AUTH-03 | Role-Based Access Control | 5 | 4.7 | Security foundation; required before any user-facing story |
| US-REC-01 | Upload Receipt via File/Drag-Drop | 3 | 4.5 | First value delivery to the user |
| US-AUTH-04 | User Invitation & Account Setup | 3 | 4.2 | Required to onboard any household member |
| US-REC-05 | Automatic Thumbnail Generation | 2 | 4.0 | Enabler — required by all receipt views |

### Tier 2 — Core Feature Delivery (Phase 1 heart)

| Story ID | Title | Points | Score | Reason |
|----------|-------|--------|-------|--------|
| US-OCR-01 | Automatic Receipt Data Extraction | 8 | 4.6 | Primary differentiator of the platform |
| US-OCR-03 | Manual Correction of Extracted Data | 5 | 4.5 | AP-005 Human Validation principle — non-negotiable |
| US-EXP-01 | Create Expense Manually | 3 | 4.3 | Covers all non-OCR expense entry (cash, etc.) |
| US-REC-02 | Upload Receipt via Mobile Camera | 5 | 3.8 | High-impact UX; most users are on mobile |
| US-AUTH-02 | Multi-Factor Authentication | 5 | 3.8 | Security hardening; follows login story |
| US-EXP-05 | View and Edit Expense History | 3 | 4.1 | Core CRUD loop — app is unusable without this |
| US-EXP-02 | Categorize and Tag an Expense | 3 | 3.9 | Required dependency for Phase 2 budgeting and search |
| US-AUTH-05 | Audit Logging | 5 | 3.7 | Owner oversight requirement |

### Tier 3 — Important but not Sprint 1–2 blockers

| Story ID | Title | Points | Score | Reason |
|----------|-------|--------|-------|--------|
| US-OCR-02 | Confidence Scoring Display | 3 | 3.5 | Enhances correction UX; depends on OCR-01 |
| US-OCR-05 | OCR Retry on Failure | 3 | 3.4 | Reliability; important but not day-1 |
| US-EXP-04 | Mark an Expense as Shared | 5 | 3.3 | Dependency for Phase 2 shared budgets |
| US-REC-03 | Attach Multiple Receipts to One Expense | 3 | 3.2 | Edge case; most receipts are single-image |
| US-EXP-06 | Item-Level Expense Breakdown | 5 | 3.1 | Nice granularity; not core flow |

### Tier 4 — Complete Phase 1 (lower urgency)

| Story ID | Title | Points | Score | Reason |
|----------|-------|--------|-------|--------|
| US-REC-04 | Image Quality Detection | 3 | 2.9 | UX improvement over hard OCR failure |
| US-OCR-04 | Barcode & QR Code Parsing | 5 | 2.8 | Good-to-have; depends on OCR-01 complete |
| US-REC-06 | Restricted Member Receipt Upload | 2 | 2.7 | Low user count role |
| US-EXP-03 | Add Notes and Attachments | 2 | 2.6 | Supplementary feature |

---

## Sprint 1
**Dates:** 2026-06-02 → 2026-06-13 (2 weeks)
**Sprint Goal:** Any household member can register, log in with enforced role permissions, and upload receipts to the system.
**Committed:** 19 points | **Stretch:** 3 points

### Committed Stories

| Story ID | Title | Points | Engineers |
|----------|-------|--------|-----------|
| US-AUTH-01 | User Login | 3 | BE + FE |
| US-AUTH-03 | Role-Based Access Control | 5 | BE |
| US-AUTH-04 | User Invitation & Account Setup | 3 | BE + FE |
| US-REC-01 | Upload Receipt via File/Drag-Drop | 3 | BE + FE |
| US-REC-05 | Automatic Thumbnail Generation | 2 | OCR |
| **Total** | | **19** | |

### Stretch Story

| Story ID | Title | Points | Condition |
|----------|-------|--------|-----------|
| US-OCR-05 | OCR Retry on Failure | 3 | Pick up if REC-05 lands by Day 8 |

---

### Engineer Task Assignments — Sprint 1

#### Senior Backend Engineer (BE)

**US-AUTH-01 — Login API**
- `POST /auth/login` — validate credentials, compare Argon2 hash, return HTTP-only session cookie
- Account lockout: reject after 5 consecutive failures, store lock timestamp in PostgreSQL
- Session config: 30-minute idle timeout, secure + httpOnly flags, SameSite=Strict

**US-AUTH-03 — Role-Based Access Control**
- Add role claim (Owner | AdultMember | RestrictedMember) to session/JWT
- ASP.NET policy middleware: enforce per-controller and per-endpoint
- All unprotected routes → 401 Unauthorized
- Role violation → 403 Forbidden with JSON body `{ "error": "Access denied" }`

**US-AUTH-04 — Invitation API**
- `POST /auth/invite` — generate a signed invite token (48h expiry), store in DB
- `POST /auth/activate` — validate token, set password (Argon2 hash), activate account
- Token expiry: auto-expire at 48h, return 410 Gone on expired token use

**US-REC-01 — Upload API**
- `POST /receipts/upload` — accept multipart form, validate MIME type (JPG/PNG/HEIC/PDF)
- Write original file to `/storage/receipts/{userId}/{uuid}.{ext}` — no lossy compression
- Return receipt record with `id`, `status: "uploaded"`, `thumbnailUrl: null` (set after OCR worker)
- Reject unsupported types with 415 + message listing accepted formats

---

#### Senior Frontend Engineer (FE)

**US-AUTH-01 — Login UI**
- `/login` page: username + password fields, submit button
- Client-side validation: both fields required before submit
- On success: redirect to `/dashboard`
- On failure: inline error "Invalid credentials" (do not reveal which field)
- On lockout: show "Account temporarily locked. Try again later."

**US-AUTH-04 — Invitation & Account Setup UI**
- `/invite/[token]` page: set-password form (password + confirm), submit
- On success: auto-login and redirect to `/dashboard` with welcome banner
- On expired token: show "This invite link has expired. Request a new one."
- Owner settings page: "Invite Member" form (name + role selector)

**US-REC-01 — Receipt Upload UI**
- Upload zone component: drag-and-drop area + "Select File" button
- Progress bar during upload (poll or streaming)
- On success: show thumbnail preview (placeholder spinner until thumbnail is ready)
- On invalid file type: inline error "Accepted formats: JPG, PNG, HEIC, PDF"
- On upload >10 MB: inline error "File too large. Maximum size is 10 MB."

---

#### Senior OCR Engineer (OCR)

**US-REC-05 — Thumbnail Worker**
- Listen on Redis queue `receipt.uploaded` events
- For each event: load file from `/storage/receipts/`, generate 300×400px thumbnail
- HEIC input → convert to JPEG using Pillow/imageio before thumbnailing
- PDF input → render first page to image (use pdf2image/poppler), then thumbnail
- Write thumbnail to `/storage/thumbnails/{receiptId}.jpg`
- Update receipt record via internal API: `PATCH /internal/receipts/{id}` with `thumbnailPath`
- Target: thumbnail ready within 2 seconds of upload

**[Stretch] US-OCR-05 — OCR Retry Logic**
- Wrap OCR job execution in a retry decorator: max 3 attempts, backoff 10s / 30s / 90s
- On each retry: update receipt status to `"processing (retry X of 3)"`
- On final failure: set status to `"ocr_failed"`, emit `receipt.ocr_failed` event for UI poll
- Log each attempt with timestamp, error type, and attempt number

---

### Sprint 1 Definition of Done

- [ ] Login works end-to-end: valid credentials → session cookie → dashboard redirect
- [ ] Unauthenticated requests to any protected route return 401
- [ ] Restricted Member cannot access Adult Member or Owner routes (403 verified in tests)
- [ ] Owner can invite a member; invite link expires after 48h
- [ ] New member can activate account via invite link and log in
- [ ] JPG, PNG, HEIC, PDF files upload successfully; unsupported types return clear error
- [ ] Thumbnail appears in UI within 2 seconds of successful upload
- [ ] All new API endpoints have integration tests
- [ ] No `console.log` or debug output in committed code

---

## Sprint 2
**Dates:** 2026-06-16 → 2026-06-27 (2 weeks)
**Sprint Goal:** Security hardening is complete (MFA + audit logging); the OCR extraction pipeline is live end-to-end so uploaded receipts auto-populate expense fields.
**Committed:** 18 points | **Stretch:** 5 points

### Committed Stories

| Story ID | Title | Points | Engineers |
|----------|-------|--------|-----------|
| US-AUTH-02 | Multi-Factor Authentication | 5 | BE + FE |
| US-AUTH-05 | Audit Logging | 5 | BE |
| US-OCR-01 | Automatic Receipt Data Extraction | 8 | OCR + BE |
| **Total** | | **18** | |

### Stretch Story

| Story ID | Title | Points | Condition |
|----------|-------|--------|-----------|
| US-REC-02 | Upload Receipt via Mobile Camera | 5 | FE picks up if MFA UI is complete by Day 7 |

> **Note on US-OCR-01 (8 pts):** Split internally — BE owns queue wiring + DB write (3 pts effort), OCR owns the extraction worker (5 pts effort). Both ship together as one story.

---

### Engineer Task Assignments — Sprint 2

#### Senior Backend Engineer (BE)

**US-AUTH-02 — MFA API**
- `POST /auth/mfa/setup` — generate TOTP secret (use `OtpNet` or equivalent), return base32 secret + otpauth URI for QR display
- `POST /auth/mfa/verify` — validate OTP against secret with ±1 window tolerance
- Add `mfa_enabled` flag + `totp_secret` (encrypted) to `users` table
- Owner can toggle MFA for any user: `PATCH /admin/users/{id}/mfa`

**US-AUTH-05 — Audit Logging**
- Create `audit_logs` table: `id`, `user_id`, `action`, `resource_type`, `resource_id`, `before_json`, `after_json`, `ip_address`, `created_at`
- Middleware: intercept all POST/PUT/PATCH/DELETE; capture before/after state; append log entry
- Append-only: no UPDATE or DELETE on audit_logs — enforced at DB level (row-level policy)
- `GET /audit` — Owner only (403 for others); supports filters: `?userId=`, `?from=`, `?to=`, `?action=`

**US-OCR-01 — BE Queue Wiring**
- On receipt upload: enqueue job to Redis stream `ocr.jobs` with `{ receiptId, filePath, userId }`
- Poll Redis stream `ocr.results` for completion; on result received: upsert `expenses` record with extracted fields and upsert `expense_items` for line items
- Update receipt status: `"processing"` → `"complete"` or `"ocr_failed"`
- Expose `GET /receipts/{id}/status` for FE polling

---

#### Senior Frontend Engineer (FE)

**US-AUTH-02 — MFA UI**
- MFA setup page `/settings/mfa`: display QR code (use `qrcode` library with otpauth URI), show backup secret as text
- Inject OTP entry step into login flow: after password validation, if `mfa_required: true` in response, route to `/login/mfa` with 6-digit input
- Owner settings: toggle MFA on/off per member with confirmation dialog

**[Stretch] US-REC-02 — Mobile Camera Upload UI**
- Add "Take Photo" button on upload page alongside existing drag-drop zone
- Use `<input type="file" accept="image/*" capture="environment">` for rear camera
- Strip EXIF on the client before upload using `piexifjs` (remove GPS + device info)
- On poor quality (flagged by BE response): show amber banner "Image may be hard to read. Retake?"

---

#### Senior OCR Engineer (OCR)

**US-OCR-01 — Extraction Worker**
- Redis stream consumer: read from `ocr.jobs`
- **Preprocessing** (OpenCV): deskew, denoise, adaptive threshold, resize to 300 DPI
- **Extraction** (Tesseract): run with `--oem 1 --psm 6`; parse output into structured fields:
  - Merchant name, address, date (parse to ISO 8601), time, subtotal, tax amount, total
  - Line items: item name, quantity, unit price (regex + positional heuristics)
- **Barcode** (ZXing): scan full image for 1D/2D codes; store decoded value if found
- **Confidence scoring**: per-field confidence (0–100); flag fields < 70 as low confidence
- **Output**: write raw OCR JSON to `/storage/ocr-json/{receiptId}.json`; push structured result to `ocr.results` Redis stream
- Target: full pipeline completes within 8 seconds

---

### Sprint 2 Definition of Done

- [ ] MFA setup generates a valid TOTP QR code that works with Google Authenticator / Authy
- [ ] Login with MFA-enabled account requires valid OTP before dashboard access
- [ ] Invalid OTP returns error; valid OTP grants session
- [ ] Every POST/PUT/PATCH/DELETE operation creates an audit log entry with before/after JSON
- [ ] `GET /audit` returns 403 for Adult Member and Restricted Member roles
- [ ] Audit log entries cannot be edited or deleted via any API endpoint
- [ ] Uploading a receipt triggers OCR; extracted fields appear in expense form within 8 seconds
- [ ] Raw OCR JSON is stored at `/storage/ocr-json/` and persists
- [ ] OCR partial failures degrade gracefully: form shows empty fields, not errors
- [ ] All new API endpoints have integration tests; OCR worker has accuracy benchmark tests

---

## Upcoming Backlog (Sprint 3 candidates)

These Tier 2 and Tier 3 stories are next in priority for sprint selection:

| Story ID | Title | Points | Priority |
|----------|-------|--------|----------|
| US-OCR-03 | Manual Correction of Extracted Data | 5 | Critical |
| US-EXP-01 | Create Expense Manually | 3 | Critical |
| US-EXP-05 | View and Edit Expense History | 3 | High |
| US-EXP-02 | Categorize and Tag an Expense | 3 | High |
| US-OCR-02 | Confidence Scoring Display | 3 | High |
| US-OCR-05 | OCR Retry on Failure | 3 | Medium (if not completed as Sprint 1 stretch) |

---

## Velocity Tracking

| Sprint | Committed | Completed | Velocity | Notes |
|--------|-----------|-----------|----------|-------|
| Sprint 1 | 19 | TBD | TBD | Baseline sprint |
| Sprint 2 | 18 | TBD | TBD | Update after sprint review |

*Update after each sprint review to track actuals.*
