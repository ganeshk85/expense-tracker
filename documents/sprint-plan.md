# Sprint Plan — Expense Tracker
**Project:** Family Expense Intelligence Platform
**Role:** Product Owner
**Date:** 2026-05-30 (updated 2026-06-27)
**Source:** documents/user-stories.md

---

## Team

| Engineer | Role | Domain |
|----------|------|--------|
| **Senior Backend Engineer (BE)** | Full-time | .NET 10 API, PostgreSQL, Redis, auth middleware |
| **Senior Frontend Engineer (FE)** | Full-time | React/Next.js, TypeScript, CSS Modules, React Query |
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

## Sprint 1 ✅ COMPLETED
**Dates:** 2026-06-02 → 2026-06-13
**Sprint Goal:** Any household member can register, log in with enforced role permissions, and upload receipts to the system.
**Committed:** 19 points | **Delivered:** 19 points

### Delivered Stories

| Story ID | Title | Points | Engineers |
|----------|-------|--------|-----------|
| US-AUTH-01 | User Login | 3 | BE + FE |
| US-AUTH-03 | Role-Based Access Control | 5 | BE |
| US-AUTH-04 | User Invitation & Account Setup | 3 | BE + FE |
| US-REC-01 | Upload Receipt via File/Drag-Drop | 3 | BE + FE |
| US-REC-05 | Automatic Thumbnail Generation | 2 | OCR |
| **Total** | | **19** | |

### Stretch Story

| Story ID | Title | Points | Outcome |
|----------|-------|--------|---------|
| US-OCR-05 | OCR Retry on Failure | 3 | Not picked up — moved to Sprint 3 |

### Sprint 1 Definition of Done — Results

- [x] Login works end-to-end: valid credentials → session cookie → dashboard redirect
- [x] Unauthenticated requests to any protected route return 401
- [x] Restricted Member cannot access Adult Member or Owner routes (403 verified in tests)
- [x] Owner can invite a member; invite link expires after 48h
- [x] New member can activate account via invite link and log in
- [x] JPG, PNG, HEIC, PDF files upload successfully; unsupported types return clear error
- [x] Thumbnail appears in UI within 2 seconds of successful upload
- [x] All new API endpoints have integration tests
- [x] No `console.log` or debug output in committed code

---

## Sprint 2 ✅ COMPLETED
**Dates:** 2026-06-16 → 2026-06-27
**Sprint Goal:** Security hardening is complete (MFA + audit logging); the OCR extraction pipeline is live end-to-end so uploaded receipts auto-populate expense fields.
**Committed:** 18 points | **Delivered:** 18 points

### Delivered Stories

| Story ID | Title | Points | Engineers |
|----------|-------|--------|-----------|
| US-AUTH-02 | Multi-Factor Authentication | 5 | BE + FE |
| US-AUTH-05 | Audit Logging | 5 | BE |
| US-OCR-01 | Automatic Receipt Data Extraction | 8 | OCR + BE |
| **Total** | | **18** | |

> **Note on US-OCR-01 (8 pts):** Split internally — BE owns queue wiring + DB write (3 pts effort), OCR owns the extraction worker (5 pts effort). Both shipped together as one story.

### Stretch Story

| Story ID | Title | Points | Outcome |
|----------|-------|--------|---------|
| US-REC-02 | Upload Receipt via Mobile Camera | 5 | **Not delivered** — FE capacity consumed by MFA UI. Moves to Sprint 4. |

### Sprint 2 Definition of Done — Results

- [x] MFA setup generates a valid TOTP QR code that works with Google Authenticator / Authy
- [x] Login with MFA-enabled account requires valid OTP before dashboard access
- [x] Invalid OTP returns error; valid OTP grants session
- [x] Every POST/PUT/PATCH/DELETE operation creates an audit log entry with before/after JSON
- [x] `GET /audit` returns 403 for Adult Member and Restricted Member roles
- [x] Audit log entries cannot be edited or deleted via any API endpoint
- [x] Uploading a receipt triggers OCR; extracted fields appear in expense form within 8 seconds
- [x] Raw OCR JSON is stored at `/storage/ocr-json/` and persists
- [x] OCR partial failures degrade gracefully: form shows empty fields, not errors
- [x] All new API endpoints have integration tests; OCR worker has accuracy benchmark tests

---

## Sprint 3 ✅ COMPLETED
**Dates:** 2026-06-16 → 2026-06-27
**Sprint Goal:** OCR results are fully reviewable and correctable; expense records can be created manually, categorized, and edited — completing the core expense lifecycle.
**Committed:** 20 points | **Delivered:** 20 points

### Delivered Stories

| Story ID | Title | Points | Engineers |
|----------|-------|--------|-----------|
| US-OCR-03 | Manual Correction of Extracted Data | 5 | BE + FE |
| US-EXP-01 | Create Expense Manually | 3 | BE + FE |
| US-EXP-05 | View and Edit Expense History | 3 | BE + FE |
| US-EXP-02 | Categorize and Tag an Expense | 3 | BE + FE |
| US-OCR-02 | Confidence Scoring Display | 3 | BE + FE + OCR |
| US-OCR-05 | OCR Retry on Failure | 3 | OCR |
| **Total** | | **20** | |

### Key Artifacts Delivered

| Layer | File / Endpoint |
|-------|----------------|
| BE | `PATCH /expenses/{id}/corrections`, `POST /expenses`, `GET/PUT /expenses`, `GET/PUT /expenses/{id}` |
| BE | Migration: `20260627000001_AddExpenseCategoryTagsNotesConfidence.cs` |
| BE | `source/api/src/ExpenseTracker.Expense/` module (Endpoints, Services, Repositories, Models) |
| OCR | `source/ocr/src/ocr_worker.py` — retry logic with 10s/30s backoff; `OcrRetryCount` on Receipt |
| FE | `source/web/src/app/expenses/page.tsx` — expense list |
| FE | `source/web/src/app/expenses/new/page.tsx` — manual create form |
| FE | `source/web/src/app/expenses/[id]/page.tsx` — detail/edit + OCR corrections |
| FE | `source/web/src/components/ReceiptStatusBadge.tsx` — retry count badge |
| FE | `source/web/src/api/expenses.ts` — typed API client |

### Sprint 3 Definition of Done — Results

- [x] OCR corrections endpoint persists corrected values and logs original OCR value
- [x] Manual expense creation (Source=Manual) works end-to-end
- [x] Expense list shows most recent first; edits reflect immediately without page refresh
- [x] Category dropdown and tag input wired to expense entity
- [x] Confidence indicators (High/Medium/Low) displayed in amber for fields < 70%
- [x] OCR retry runs up to 3 times with exponential backoff; UI shows retry count

---

## Post-Sprint 3 Backfill — Navigation & Receipt Upload UI ✅ COMPLETED
**Date:** 2026-06-27
**Trigger:** Sprint 3 review identified that the US-REC-01 frontend deliverable was never built — the backend upload endpoint and API types existed, but no receipt upload page or application navigation had been created. Pages built in Sprints 1–3 were unreachable without typing URLs directly.

**Not a new story — this is correction of an incomplete Sprint 1 FE delivery.**

### Work Done

| Layer | File | Description |
|-------|------|-------------|
| FE | `source/web/src/components/NavSidebar.tsx` | Sidebar with SVG icons; active state via `usePathname` |
| FE | `source/web/src/components/NavSidebar.module.css` | Sidebar styles |
| FE | `source/web/src/components/AppShell.tsx` | Client shell; hides sidebar on `/login` and `/invite/*` |
| FE | `source/web/src/components/AppShell.module.css` | Shell layout (flex row) |
| FE | `source/web/src/app/layout.tsx` | Updated to wrap children with `AppShell` |
| FE | `source/web/src/app/receipts/upload/page.tsx` | Full upload page: drag-drop, file picker, upload, OCR polling, all states |
| FE | `source/web/src/app/receipts/upload/upload.module.css` | Upload page styles |
| FE | `source/web/src/api/receipts.ts` | `uploadReceipt()` and `getReceiptStatus()` API client functions |

### Upload Page States
1. **Idle** — drag-drop zone with file picker; client-side MIME + size validation
2. **Uploading** — spinner while `POST /receipts/upload` is in flight
3. **Processing** — polls `GET /receipts/{id}/status` every 2 seconds; shows thumbnail when available; shows retry badge if OCR is retrying
4. **Complete** — success card with thumbnail + "View Expenses" and "Upload Another" actions
5. **Failed** — error card with "Add Manually" (→ `/expenses/new`) and "Try Again" actions

### Root Cause Note
The Sprint 1 DoD checklist was marked complete based on backend delivery. The FE receipt upload page was assigned but not implemented. Going forward: DoD verification must include a browser smoke test before marking a story done.

---

## Sprint 4 — Phase 1: Shared Expenses + Receipt Flexibility
**Dates:** 2026-06-30 → 2026-07-11
**Sprint Goal:** Members can share and split expenses; receipts can be captured via mobile camera and attached in multiples; OCR line items are fully editable.
**Committed:** 18 points | **Stretch:** 0 points

### Committed Stories

| Story ID | Title | Points | Engineers | Rationale |
|----------|-------|--------|-----------|-----------|
| US-EXP-04 | Mark an Expense as Shared | 5 | BE + FE | Highest priority — prerequisite for Phase 2 shared budgets |
| US-REC-02 | Upload Receipt via Mobile Camera | 5 | FE + OCR | High-impact UX; most users are on mobile; missed Sprint 2 stretch |
| US-REC-03 | Attach Multiple Receipts to One Expense | 3 | BE + FE | Completes receipt capture flexibility |
| US-EXP-06 | Item-Level Expense Breakdown | 5 | BE + FE | OCR line items from Sprint 2 are ready; unblocked by EXP-05 |
| **Total** | | **18** | | |

### Engineer Task Assignments — Sprint 4

#### Senior Backend Engineer (BE)

**US-EXP-04 — Shared Expense API**
- Add `is_shared` flag and `expense_shares` join table: `expense_id`, `user_id`, `amount`, `percentage`
- `POST /expenses/{id}/shares` — assign members and split amounts/percentages; validate shares sum to total
- `GET /expenses` — for Adult Member: return own expenses + shared expenses they are part of
- `GET /expenses` — for Restricted Member: return only assigned expenses (no shared expenses unless explicitly included)
- `GET /expenses` — for Owner: support `?view=household` to see all household expenses
- Share recalculation: `PUT /expenses/{id}` — if total changes and shares exist, return 409 with `shares_out_of_sync: true` so FE can prompt user
- Audit log: expense share creation/modification must generate audit entries

**US-REC-03 — Multiple Receipts API**
- Extend `receipts` table: add `expense_id` FK (nullable — receipt can be unlinked or linked to an expense)
- `POST /expenses/{id}/receipts` — attach an already-uploaded receipt to an expense
- `DELETE /expenses/{id}/receipts/{receiptId}` — detach a receipt; do not delete the underlying file
- `GET /expenses/{id}` — include `receipts[]` array with `id`, `thumbnailUrl`, `status`
- When multiple receipts are attached: each triggers its own OCR job; results are merged (fields from highest-confidence receipt win where conflicts exist)

**US-EXP-06 — Item-Level Breakdown API**
- `expense_items` table already exists (created Sprint 2 for OCR); expose full CRUD
- `GET /expenses/{id}/items` — return all line items for an expense
- `POST /expenses/{id}/items` — add a manual line item (`name`, `quantity`, `unit_price`)
- `PUT /expenses/{id}/items/{itemId}` — update item
- `DELETE /expenses/{id}/items/{itemId}` — remove item; recalculate and return updated total
- Validation: if line items exist and sum ≠ expense total, return `items_total_mismatch: true` in GET response (do not block save)

---

#### Senior Frontend Engineer (FE)

**US-EXP-04 — Shared Expense UI**
- Add "Shared Expense" toggle on expense edit form
- When toggled on: show member selector (multi-select from household members) and split input (amount or percentage per member)
- Auto-fill equal split when members are selected; allow manual override
- Validation: splits must sum to total before form can be saved; show running sum indicator
- Shared expense card in list view: show shared badge and member avatars/initials
- Owner expense list: "All Household" toggle button in list header

**US-REC-02 — Mobile Camera Upload UI**
- Add "Take Photo" button alongside existing drag-drop zone on upload page
- Use `<input type="file" accept="image/*" capture="environment">` for rear camera activation
- Strip EXIF on the client before upload using `piexifjs` (remove GPS + device info)
- On poor quality (flagged by BE response): show amber banner "Image may be hard to read. Retake?"
- "Choose from Gallery" option: `<input type="file" accept="image/*">` (no capture attribute)
- Upload flow identical to desktop after image is selected

**US-REC-03 — Multiple Receipts UI**
- Expense detail page: show receipt gallery (horizontal scroll, thumbnail cards)
- "Attach another receipt" button opens upload zone inline (no page navigation)
- Each thumbnail card: remove button (x) with confirmation dialog
- Gallery shows max 5 thumbnails in row; overflow → "View all (N)" link

**US-EXP-06 — Item-Level Breakdown UI**
- Expand existing expense detail page with collapsible "Line Items" section
- Editable table: item name, quantity, unit price; auto-computed row total
- "Add item" row at the bottom; inline delete per row
- Running total footer: show items sum; highlight in amber if sum ≠ expense total
- Show warning message (not block) if totals mismatch on save

---

#### Senior OCR Engineer (OCR)

**US-REC-02 — EXIF Handling Verification**
- Verify Python worker does not re-embed GPS or device metadata when writing thumbnails
- Add `piexif.remove()` call on any HEIC→JPEG conversion path to strip EXIF at server side as backup
- Add test: upload image with GPS EXIF, assert stored file and thumbnail have no GPS tags

**US-REC-03 — Multi-Receipt OCR Merge**
- When multiple receipts are attached to one expense: process each in parallel (separate Redis jobs)
- Merge strategy: for each field, pick the value from the receipt with the highest per-field confidence score
- Log merge decisions to OCR JSON output: `{ "field": "total", "source_receipt_id": "...", "confidence": 94 }`

---

### Sprint 4 Definition of Done

- [ ] Shared expense toggle saves split data; all named members see their share in their expense list
- [ ] Restricted Member cannot see shared expenses unless explicitly included by Owner
- [ ] Owner "All Household" view shows every expense across all members
- [ ] Expense total change with active shares returns `shares_out_of_sync` signal; UI prompts re-split
- [ ] Mobile camera capture activates rear camera; EXIF GPS data is stripped before upload
- [ ] Multiple receipts attach to one expense; removing one does not affect others
- [ ] Line items table is editable; deleting an item recalculates expense total
- [ ] Line item sum ≠ total shows amber warning; save is not blocked
- [ ] All new endpoints have integration tests
- [ ] No `console.log` or debug output in committed code

---

## Sprint 5 — Phase 1 Completion + Phase 2 Kickoff
**Dates:** 2026-07-14 → 2026-07-25
**Sprint Goal:** Phase 1 is fully delivered; the search foundation and first budget entity are live to unblock Phase 2.
**Committed:** 20 points

### Committed Stories

| Story ID | Title | Points | Engineers | Phase |
|----------|-------|--------|-----------|-------|
| US-OCR-04 | Barcode & QR Code Parsing | 5 | OCR + BE | 1 |
| US-REC-04 | Image Quality Detection | 3 | OCR + FE | 1 |
| US-REC-06 | Restricted Member Receipt Upload | 2 | BE + FE | 1 |
| US-EXP-03 | Add Notes and Attachments | 2 | BE + FE | 1 |
| US-BUD-01 | Set Monthly Category Budget | 3 | BE + FE | 2 — pairs with lighter Phase 1 tail |
| US-SRCH-01 | Multi-Field Expense Search | 5 | BE | 2 — search infra is foundational for dashboard |
| **Total** | | **20** | | |

> **Phase 1 closes at end of Sprint 5.** All 84 Phase 1 points delivered across 5 sprints.

### Engineer Task Assignments — Sprint 5

#### Senior Backend Engineer (BE)

**US-OCR-04 — Barcode/QR Storage**
- `receipts` table already has `barcode_value` column (added Sprint 2 by OCR worker)
- Expose `GET /receipts/{id}` — include `barcodeValue` and `barcodeType` (QR / EAN-13 / etc.) when present
- `GET /expenses/{id}` — surface barcode fields if linked receipt has one
- No barcode field in API response if value is null (omit key entirely, not null)

**US-REC-06 — Restricted Member Upload**
- Receipt upload endpoint already supports all roles — confirm RBAC policy allows RestrictedMember on `POST /receipts/upload`
- `GET /expenses` for Restricted Member: filter to `assigned_to = current_user_id` only
- `GET /receipts` for Restricted Member: return only receipts they uploaded
- Test: Restricted Member upload → expense created → visible only to uploader and Owner/AdultMember who reviews it

**US-EXP-03 — Notes & Attachments API**
- `notes` column already exists on `expenses` table (added Sprint 3)
- `attachments` table: `id`, `expense_id`, `file_name`, `file_path`, `file_size`, `mime_type`, `created_at`
- `POST /expenses/{id}/attachments` — accept any MIME type up to 10 MB; write to `/storage/attachments/{expenseId}/{uuid}.{ext}`
- `GET /expenses/{id}/attachments` — return list with `id`, `fileName`, `downloadUrl`, `fileSize`
- `DELETE /expenses/{id}/attachments/{id}` — delete file and record
- 10 MB limit: return 413 with `"File exceeds 10 MB limit"` message

**US-BUD-01 — Budget API**
- `budgets` table: `id`, `household_id`, `category`, `monthly_limit`, `effective_month`, `created_by`, `created_at`
- `POST /budgets` — Owner only (403 for others); validate `monthly_limit > 0`
- `GET /budgets` — return all active budgets for household; include `spent` (sum of expenses in category for current month) and `progress_percent`
- `PUT /budgets/{id}` — update limit; recalculate progress immediately
- `GET /budgets/{id}` — single budget with progress bar data

**US-SRCH-01 — Search API**
- PostgreSQL full-text search across `expenses`: merchant, notes, tags (via tsvector column)
- `GET /expenses/search?q=&category=&from=&to=&minAmount=&maxAmount=&tags=`
- Response within 1 second (add GIN index on tsvector column)
- Restricted Member: results filtered to assigned expenses only
- Return `total_count` and paginated results (`?page=&pageSize=`)

---

#### Senior Frontend Engineer (FE)

**US-REC-04 — Image Quality Warning UI**
- After upload: if BE response includes `imageQuality: "low"`, show amber banner: "This image may be hard to read. Consider retaking it."
- Banner has "Dismiss" and "Replace Image" actions
- "Replace Image" re-opens upload zone inline without navigating away
- No banner for `imageQuality: "ok"` or when field is absent

**US-REC-06 — Restricted Member Upload UI**
- Upload page already functional — verify it is accessible to RestrictedMember role
- Post-upload: show expense detail in read-only mode (no edit controls); show "Pending Review" status badge
- Expense list for Restricted Member: "My Uploads" label instead of "My Expenses"; filter is enforced server-side

**US-EXP-03 — Notes & Attachments UI**
- Notes: existing textarea on expense edit form — confirm it saves correctly (wired in Sprint 3 entity but may not be surfaced in form)
- Attachments: add file drop zone below notes section; show attached file list (name + size + delete button)
- File size validation on client: reject > 10 MB with inline error before upload attempt
- Download link for each attachment: opens in new tab

**US-BUD-01 — Budget Settings UI**
- `/settings/budgets` page: list of category budgets with progress bars (spent / limit)
- "Add Budget" form: category selector + monthly limit input
- Edit inline: click limit amount to edit in place, press Enter to save
- Progress bar: green < 80%, amber 80–99%, red ≥ 100%

---

#### Senior OCR Engineer (OCR)

**US-OCR-04 — Barcode & QR Worker**
- ZXing already integrated (Sprint 2); ensure all 1D and 2D code types are enabled
- Scan full image after preprocessing; store `{ value, type }` in OCR JSON output
- Push `barcodeValue` and `barcodeType` fields in `ocr.results` Redis message
- Performance: barcode scan must not extend total OCR time past 8-second target (run in parallel with Tesseract)
- No barcode found → omit field from result (do not emit null)

**US-REC-04 — Image Quality Detection Worker**
- After `receipt.uploaded` event: run blur detection (Laplacian variance) and brightness check
- Thresholds: blur variance < 100 → low quality; mean pixel brightness < 40 → low quality
- Emit `PATCH /internal/receipts/{id}` with `imageQuality: "low" | "ok"` before OCR job is enqueued
- Target: quality check completes within 500ms of upload

---

### Sprint 5 Definition of Done

- [ ] Barcodes and QR codes decoded from receipts appear in receipt detail view; absent if not found
- [ ] Blurry or dark image upload shows quality warning banner; dismissible; OCR still proceeds
- [ ] Restricted Member can upload receipts; expense is visible only to them and Owners
- [ ] Notes save correctly from expense edit form; attachments upload, download, and delete
- [ ] Attachment > 10 MB rejected client-side and server-side with clear error
- [ ] Budget can be set per category with monthly limit; progress bar reflects current-month spend
- [ ] Budget limit of 0 is rejected with validation error
- [ ] Expense search returns results within 1 second; filters combine correctly
- [ ] Restricted Member search returns only assigned expenses
- [ ] All new endpoints have integration tests

---

## Sprint 6 — Phase 2: Budgeting
**Dates:** 2026-07-28 → 2026-08-08
**Sprint Goal:** Household budget management is fully operational — shared budgets, threshold alerts, automatic monthly resets, and a spending summary dashboard are live.
**Committed:** 21 points

### Committed Stories

| Story ID | Title | Points | Engineers |
|----------|-------|--------|-----------|
| US-BUD-02 | Household Shared Budget | 5 | BE + FE |
| US-BUD-03 | Budget Threshold Alerts | 5 | BE + FE |
| US-BUD-04 | Monthly Budget Reset | 3 | BE |
| US-SRCH-02 | Spending Summary Dashboard | 5 | BE + FE |
| US-SRCH-05 | Export Expense Report (CSV) | 3 | BE + FE |
| **Total** | | **21** | |

> If team is over-committed, drop US-SRCH-05 (3 pts) to Sprint 7 — it has no downstream dependencies.

### Engineer Task Assignments — Sprint 6

#### Senior Backend Engineer (BE)

**US-BUD-02 — Shared Household Budget**
- `budgets` table: add `type` column — `category` (existing) or `household`
- Household budget: not scoped to a single category; tracks all spending across members
- `GET /budgets` — return household budgets alongside category budgets; include per-member breakdown for Owner view
- Member breakdown: `{ userId, displayName, contributed: amount }` — Owner only; other roles see aggregate only
- `DELETE /budgets/{id}` — trigger in-app notification to all Adult Members: "Shared budget was removed by Owner"

**US-BUD-03 — Budget Alerts**
- Background job (Redis-triggered after each expense write): recalculate budget progress
- If progress crosses 80% threshold: create notification record in `notifications` table
- If progress crosses 100%: create separate "exceeded" notification
- `notifications` table: `id`, `user_id`, `type`, `message`, `budget_id`, `created_at`, `dismissed_at`
- `POST /notifications/{id}/dismiss` — set `dismissed_at`; alert does not re-fire until next monthly cycle

**US-BUD-04 — Monthly Budget Reset**
- Cron job (midnight on 1st of each month): snapshot current month's budget progress to `budget_history` table, then reset
- `budget_history`: `id`, `budget_id`, `month`, `limit`, `spent`, `created_at` — append-only
- On reset failure: log error, retry after 5 minutes (up to 3 attempts); emit alert to Owner via notification
- `GET /budgets/history?month=` — return historical snapshots for a given month

**US-SRCH-02 — Dashboard API**
- `GET /dashboard/summary?month=YYYY-MM` — return: total spent, breakdown by category (amount + percentage), top 5 merchants, expense count
- Owner: supports `?view=household` for all-member aggregation
- Adult Member: own expenses + shared expenses they are part of
- Response cached in Redis for 60 seconds (cache key: `dashboard:{userId}:{month}:{view}`)
- Cache invalidated on any expense write for that user

**US-SRCH-05 — CSV Export**
- `GET /expenses/export?from=&to=` — stream CSV response with `Content-Disposition: attachment; filename=expenses-{from}-{to}.csv`
- Columns: Date, Merchant, Category, Tags, Amount, Currency, Source (Manual/OCR), Notes
- Restricted Member: own expenses only; Adult Member: own + shared
- Empty range → empty CSV with header row (no error)

---

#### Senior Frontend Engineer (FE)

**US-BUD-02 — Shared Budget UI**
- Budget settings page: add "Household Budget" section separate from category budgets
- Household budget card: total progress + per-member bar breakdown (Owner only; other roles see aggregate)
- "Budget removed" in-app notification: banner at top of screen, auto-dismiss after 10 seconds

**US-BUD-03 — Alert UI**
- Notification bell icon in nav header: badge count of unread alerts
- `/notifications` page: list of budget alerts (date, category, message); dismiss button per alert
- Alert banner on budget card when category is ≥ 80%: amber for threshold, red for exceeded

**US-SRCH-02 — Dashboard UI**
- `/dashboard` page: total spend card, category breakdown donut chart, top merchants list
- Month selector: current month default; previous months accessible via dropdown
- Owner: "Household / My View" toggle
- Empty state: "$0 total — add expenses to see your summary"
- Load within 3 seconds (Redis-cached endpoint)

**US-SRCH-05 — Export UI**
- "Export CSV" button on expense list page (header area)
- Date range picker modal: from/to inputs, "Export" CTA
- On success: file downloads automatically; no navigation change
- On empty range: file still downloads (empty CSV with headers)

---

### Sprint 6 Definition of Done

- [x] Shared household budget visible to all Adult Members with aggregate progress
- [x] Owner sees per-member contribution breakdown on household budget
- [x] Budget threshold alert fires at 80% and 100%; dismissed alerts do not re-fire within same month cycle
- [x] Budget resets automatically on the 1st of each month; historical data is preserved in `budget_history`
- [x] Reset failure is retried and Owner is notified via in-app notification
- [x] Dashboard loads within 3 seconds; shows total spend and category breakdown for selected month
- [x] Owner household view aggregates all member spending
- [x] CSV export downloads correctly; includes all specified columns; empty range returns header-only file
- [ ] All new endpoints have integration tests

---

## Sprint 7 — Phase 2: Analytics + Hardening
**Dates:** 2026-08-11 → 2026-08-22
**Sprint Goal:** Spending trend and merchant analytics complete the Phase 2 intelligence layer. Second half of sprint is a hardening buffer — integration testing, performance tuning, and tech debt.
**Committed:** 18 points (11 pts stories + 7 pts hardening)

### Committed Stories

| Story ID | Title | Points | Engineers |
|----------|-------|--------|-----------|
| US-SRCH-03 | Category Trend Report | 5 | BE + FE |
| US-SRCH-04 | Merchant Analytics | 3 | BE + FE |
| **Hardening** | Integration tests, load testing, performance tuning, tech debt | ~10 | ALL |
| **Total** | | **18** | |

> Sprint 7 is intentionally lighter on new stories. Six consecutive delivery sprints warrant a controlled buffer before Phase 3 planning begins.

### Engineer Task Assignments — Sprint 7

#### Senior Backend Engineer (BE)

**US-SRCH-03 — Trend API**
- `GET /analytics/trends?category=&months=6` — return monthly totals per category for the past N months
- Each month: `{ month: "YYYY-MM", amount, expenseCount }`
- Spike detection: if month-over-month increase > 20%, include `spike: true` flag on that month
- Minimum data guard: if < 2 months of data, return `{ insufficient_data: true }` — no empty arrays

**US-SRCH-04 — Merchant API**
- `GET /analytics/merchants?from=&to=` — ranked list of merchants by total spend descending
- Each entry: `{ merchant, totalSpent, visitCount, lastVisit }`
- No minimum visit threshold — single-visit merchants appear in the list
- Clicking a merchant: `GET /expenses?merchant={name}&from=&to=` (reuse existing search endpoint)

**Hardening**
- Load test search and dashboard endpoints: target < 1s search, < 3s dashboard at 50 concurrent users
- Add database indexes if query plans show seq scans on `expenses` (merchant, category, date columns)
- Review Redis cache hit rates; tune TTLs based on actual usage patterns

---

#### Senior Frontend Engineer (FE)

**US-SRCH-03 — Trend Chart UI**
- `/analytics` page: bar or line chart (use a lightweight chart library — Recharts or Chart.js)
- Category filter: dropdown to focus chart on a single category
- Spike months: visually highlighted bar (different colour or pattern)
- Tooltip on hover: exact amount and month label
- Insufficient data: replace chart with "Not enough data yet — add more expenses to see trends."

**US-SRCH-04 — Merchant UI**
- `/analytics/merchants` page: ranked table — merchant name, total spent, visit count, last visit date
- Date range filter at top of page
- Row click → navigates to filtered expense list for that merchant

**Hardening**
- Lighthouse audit on dashboard and expense list: target performance score ≥ 85
- Accessibility pass: keyboard navigation, ARIA labels on charts and badges
- Fix any TypeScript strict-mode warnings introduced in Sprints 4–6

---

#### Senior OCR Engineer (OCR)

**Hardening**
- Run OCR accuracy benchmark across 50 test receipts; document baseline accuracy per field
- Profile Python worker memory usage under sustained load (20 concurrent jobs)
- Clean up any temporary files left in `/storage` by failed jobs

---

### Sprint 7 Definition of Done

- [ ] Category trend chart renders 6-month data; spikes (>20% MoM) are visually flagged
- [ ] Insufficient data state shows message instead of empty chart
- [ ] Merchant rankings list is accurate and sortable by date range
- [ ] Search responds in < 1 second at 50 concurrent users (load test passing)
- [ ] Dashboard responds in < 3 seconds at 50 concurrent users
- [ ] No TypeScript strict-mode warnings in committed code
- [ ] OCR accuracy baseline documented for Sprint 8+ Phase 3 reference

---

## Phase 2 Complete — End of Sprint 7 (2026-08-22)

All 37 Phase 2 points delivered. Platform has full budget management, analytics, and search. Phase 3 (Intelligence) and Phase 4 (Mobile) planning to begin after Sprint 7 retrospective.

**Phase 3 scope (not yet estimated):** merchant template learning, advanced receipt parsing, ML-assisted local parsing.
**Phase 4 scope (not yet estimated):** React Native + Expo, offline sync.

---

## Velocity Tracking

| Sprint | Committed | Delivered | Velocity | Notes |
|--------|-----------|-----------|----------|-------|
| Sprint 1 | 19 | 19 | 19 | Baseline sprint |
| Sprint 2 | 18 | 18 | 18 | REC-02 stretch not delivered — FE at capacity |
| Sprint 3 | 20 | 20 | 20 | All 6 stories delivered |
| Sprint 4 | 18 | TBD | TBD | Update after sprint review |
| Sprint 5 | 20 | TBD | TBD | Update after sprint review |
| Sprint 6 | 21 | 21 | 21 | All 5 stories delivered; integration tests pending |
| Sprint 7 | 18 | TBD | TBD | Update after sprint review |

**Established velocity:** ~19 pts/sprint (18–20 range, 3-sprint average)
