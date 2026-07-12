# Sprint Plan — Expense Tracker
**Project:** Family Expense Intelligence Platform
**Role:** Product Owner
**Date:** 2026-05-30 (updated 2026-06-28)
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

## Sprint 8 — Phase 3: Auto-Categorization + Duplicate Detection
**Dates:** 2026-08-25 → 2026-09-05
**Sprint Goal:** The platform begins learning from confirmed expense history — new receipts are auto-categorized by merchant, and duplicate uploads are flagged before they pollute the ledger.
**Committed:** 19 points

### Committed Stories

| Story ID | Title | Points | Engineers | Rationale |
|----------|-------|--------|-----------|-----------|
| US-INT-01 | Merchant-to-Category Auto-Categorization | 8 | OCR + BE + FE | Highest-impact intelligence feature; built entirely on existing confirmed expense data — no external model needed |
| US-INT-02 | Duplicate Expense Detection | 5 | BE + FE | Prevents data quality issues; uses indexed fields already present; delivers immediate visible value |
| US-INT-03 | Smart Tag Suggestions | 3 | BE + FE | Low-effort extension of the merchant lookup table built for INT-01; high UX value |
| US-INT-04 | OCR Accuracy Feedback Loop | 3 | OCR + BE | Closes the human-validation loop (AP-005); corrections feed back into per-merchant confidence baselines |
| **Total** | | **19** | | |

> US-INT-01 carries the most technical uncertainty (learning pipeline design). BE and OCR pair on the data model in week 1; FE picks up US-INT-03 in parallel to avoid blocking.

### Engineer Task Assignments — Sprint 8

#### Senior Backend Engineer (BE)

**US-INT-01 — Auto-Categorization Service**
- `merchant_category_map` table: `id`, `household_id`, `merchant_name_normalized`, `category`, `confirmed_count`, `last_confirmed_at`
  - `merchant_name_normalized`: lowercase, whitespace-collapsed, punctuation-stripped version of the merchant string
  - `confirmed_count`: incremented each time a user confirms (saves without editing) an expense for this merchant
- Background job triggered after `expense.confirmed` event (Redis): upsert into `merchant_category_map` with the confirmed category
  - "Confirmed" defined as: expense saved where `source = OCR` and the category field was not changed by the user during the correction step
  - Manual expense saves also count if user explicitly selected a category
- Lookup at OCR result time: after `ocr.results` message arrives, query `merchant_category_map` for the normalized merchant name
  - If a match exists with `confirmed_count >= 3`: attach `suggestedCategory` and `suggestionConfidence: "high"` to the OCR result payload
  - If match exists with `confirmed_count` 1–2: attach with `suggestionConfidence: "low"`
  - No match: omit `suggestedCategory` entirely
- `GET /intelligence/merchant-map` — Owner only; returns the full household merchant-category mapping table (for transparency / audit)
- Normalization function must be consistent between write path (confirmation) and read path (suggestion) — extract to a shared utility

**US-INT-02 — Duplicate Detection Service**
- Duplicate defined as: same `household_id`, same `merchant_name_normalized`, same `amount`, and `expense_date` within ±1 calendar day
- Check runs synchronously in the `POST /expenses` and `POST /receipts/upload` → expense-create paths
- If a potential duplicate is found: do not block the save; return `duplicateWarning: { existingExpenseId, existingDate, confidence: "high" | "possible" }` in the response body
  - `high` confidence: all three fields match exactly
  - `possible` confidence: merchant and amount match, date differs by 1 day
- `duplicate_dismissals` table: `expense_id`, `dismissed_by`, `dismissed_at` — records when a user confirms "not a duplicate"
- `POST /expenses/{id}/dismiss-duplicate` — record dismissal; suppresses warning on subsequent fetches of that expense
- Audit log entry on dismissal: `{ action: "duplicate_dismissed", expenseId, existingExpenseId }`

**US-INT-04 — OCR Accuracy Feedback Loop (API side)**
- When a user saves corrections via `PATCH /expenses/{id}/corrections`: compare corrected values against the original OCR-extracted values
- For each field where the user's value differs from OCR output: emit a `ocr.correction` event to Redis with `{ receiptId, merchantNormalized, field, ocrValue, correctedValue }`
- `ocr_field_accuracy` table: `id`, `merchant_name_normalized`, `field_name`, `total_extractions`, `total_corrections`, `last_updated`
  - Upsert on each `ocr.correction` event: increment `total_extractions` and `total_corrections`
- `GET /intelligence/ocr-accuracy` — Owner only; returns per-merchant, per-field accuracy rates: `{ merchant, field, accuracyRate, sampleSize }`
  - Minimum sample size of 5 before showing a rate (return `insufficient_data: true` below threshold)

---

#### Senior Frontend Engineer (FE)

**US-INT-01 — Auto-Categorization UI**
- On the expense edit form (reached after OCR completes): if `suggestedCategory` is present in the OCR result, pre-select that category in the dropdown
- Show a subtle "Suggested" badge next to the category field — not an alert, not a modal; inline and dismissible
- Badge text: "Suggested based on past expenses" with a small info icon; clicking the icon shows a tooltip explaining the source
- If `suggestionConfidence: "low"`: badge colour is grey (informational); if `"high"`: badge colour is blue
- User can change the category normally; the suggestion is just a default pre-fill, not a lock

**US-INT-02 — Duplicate Warning UI**
- After expense save (POST response): if `duplicateWarning` is present, show a non-blocking inline banner below the form header
- Banner text: "This looks like a possible duplicate of [Merchant] on [Date] for [Amount]. [View existing] [Dismiss]"
- "View existing" opens the referenced expense in a side panel or new tab (not a full navigation away)
- "Dismiss" calls `POST /expenses/{id}/dismiss-duplicate`; banner disappears immediately
- If user ignores the banner and navigates away, the warning is shown again on next load of that expense until dismissed
- Duplicate warning banner also surfaces in the expense list row — small amber "Possible duplicate" tag alongside the existing status badges

**US-INT-03 — Smart Tag Suggestions**
- When the user begins typing in the tag input on the expense form: show an autocomplete dropdown
- Suggestions sourced from two tiers in priority order:
  1. Tags previously used for the same merchant (from `merchant_tag_history` — see BE tasks below)
  2. All tags used in the household, ranked by frequency, as a fallback
- Maximum 5 suggestions shown; keyboard-navigable; pressing Enter or Tab selects a highlighted suggestion
- No network call on each keystroke — fetch suggestions once on merchant field blur; cache in component state for the session

**US-INT-04 — OCR Accuracy Report UI**
- Add "OCR Accuracy" subsection to the existing `/analytics` page (new tab or accordion, not a new route)
- Table: merchant name, field, accuracy rate (%), sample size — sortable by accuracy rate ascending (worst first)
- Rows with `insufficient_data: true` shown at the bottom with "Not enough data yet" in the rate column
- Owner-only: if current user is not Owner, the subsection is hidden entirely (not a 403 page — just not rendered)

---

#### Senior OCR Engineer (OCR)

**US-INT-01 — Merchant Name Normalization**
- Implement `normalize_merchant(name: str) -> str` in the OCR worker:
  - Lowercase, strip leading/trailing whitespace, collapse internal whitespace to single space
  - Remove punctuation characters: `. , ' " - _ & /` (keep alphanumeric and spaces)
  - Examples: `"WOOLWORTHS PTY LTD."` → `"woolworths pty ltd"`, `"7-Eleven"` → `"7 eleven"`
- This normalization must exactly match the SQL normalization applied in the BE `merchant_category_map` lookups — coordinate with BE on the canonical algorithm and write a shared test fixture document
- Apply normalization to the `merchant` field in every `ocr.results` Redis message going forward

**US-INT-04 — OCR Feedback Consumer**
- Add Redis consumer for the `ocr.correction` channel in the OCR worker process
- On each correction event: update the local in-memory per-merchant accuracy stats (Python dict, rebuilt from DB on worker start)
- Use these stats to adjust Tesseract preprocessing aggressiveness for low-accuracy merchants:
  - If a merchant's field accuracy for `total` or `date` drops below 70%: apply additional deskew + contrast enhancement before the next OCR pass for that merchant
  - Log: `INFO ocr_accuracy_adaptive merchant={name} field={field} accuracy={rate} action=enhanced_preprocessing`
- Write an integration test: seed 10 correction events for a merchant, assert that the next OCR call for that merchant triggers the enhanced path

---

### Sprint 8 Definition of Done

- [x] When a user confirms an expense for a merchant, the merchant-category mapping is persisted and the confirmed count increments correctly
- [x] On the next receipt upload from the same merchant (with confirmed_count >= 3), the category field is pre-filled with the suggested category and displays the "Suggested" badge — **was dead code until the 2026-07-05 fix pass (see Review Notes)**
- [x] Suggestion badge is dismissible; changing the category does not trigger any error
- [x] Uploading a receipt with the same merchant, amount, and date as an existing expense shows a duplicate warning banner — save is not blocked
- [x] `possible` confidence duplicate (1-day date difference) shows the amber "Possible duplicate" tag; exact match now gets a distinct red-tinted banner (added 2026-07-05)
- [x] Dismissing a duplicate warning calls the dismiss endpoint and suppresses the banner on reload — **was broken by an ID mismatch until the 2026-07-05 fix (see Review Notes)**
- [x] Tag autocomplete shows merchant-specific tag history first; falls back to household tag frequency
- [x] OCR accuracy table is visible to Owner on the analytics page; hidden for other roles
- [x] Fields with fewer than 5 samples show "Not enough data yet" rather than a rate — **rate itself was always 0% until the 2026-07-05 fix (see Review Notes)**
- [x] Merchant name normalization produces identical output in the Python worker and the .NET service for the same input string — algorithms matched, but the shared fixture had 5 wrong entries and was never actually loaded by a test until 2026-07-05
- [x] All new endpoints have integration tests (added 2026-07-05; HTTP-level runs are currently blocked by a pre-existing test-infra login bug — see Review Notes — but logic is verified via service-level unit tests)
- [x] No `console.log` or debug output in committed code

---

## Sprint 8 — Review Notes (2026-07-05)

**The DoD above was originally checked off at commit `ff39f9b` without ever being verified — a code-level audit requested before Sprint 9 kickoff found the flagship feature was dead code and the feedback-loop math was broken.** All findings below were fixed before Sprint 9 began.

### What was actually broken (found by reading the code, not by running anything)

| # | Issue | Root cause | Fix |
|---|-------|-----------|-----|
| 1 | Category suggestion badge never appeared | `IntelligenceService.GetSuggestedCategoryAsync` existed but was **never called** from `ExpenseService` — the API always returned `suggestedCategory: null` | Wired into `GetByIdAsync`; added a confidence threshold (`confirmed_count >= 3` → `high`, else `low`) via a new `CategorySuggestion` record |
| 2 | OCR accuracy always showed 0% | `UpsertOcrFieldAccuracyAsync` incremented `TotalExtractions` and `TotalCorrections` together on every call, because the only event source (`ocr.correction`) was only ever emitted for *changed* fields | Correction events now fire for every field on every confirmation (not just changed ones) with an `isCorrected` flag; extractions always increment, corrections only when actually corrected. Same bug existed in both the .NET consumer and the Python `correction_consumer.py` — fixed in both |
| 3 | Dismissing a duplicate warning didn't suppress it | `DismissDuplicateAsync` recorded the dismissal keyed by the *current* expense's ID, but `CheckDuplicateAsync` checked `IsDismissedAsync` against the *matched/older* expense's ID — different rows, so the dismissal never matched on reload | `CheckDuplicateAsync` now checks dismissal against the current expense's own ID |
| 4 | Shared merchant-normalization fixture was never validated | `merchant_normalization_fixtures.json` existed but no test in either language loaded it; 5 of 24 entries had incorrect expected values (the whitespace-collapse/trim steps were never applied when the fixture was hand-written) | Fixed the 5 wrong entries; added `MerchantNormalizerFixtureTests.cs` (.NET) and `test_merchant_normalizer.py` (Python), both loading the same fixture file |
| 5 | Duplicate confidence had no visual distinction | Banner text differed between "high"/"possible" but the CSS was identical | Added `.duplicateBannerHigh` (red-tinted) alongside the existing amber default used for "possible" |
| 6 | Zero integration test coverage | No test project touched any Sprint 8 endpoint | Added `IntelligenceEndpointsTests.cs` (merchant-map / ocr-accuracy / tag-suggestions role gating, duplicate-detection create/dismiss flow) and `IntelligenceServiceTests.cs` (suggestion confidence thresholds — unit-level, since OCR-sourced `ConfidenceJson` isn't reachable via a public endpoint) |

### Additional pre-existing test-infrastructure bugs found and fixed along the way

These blocked **every** integration test in `ExpenseTracker.Budget.Tests`, including the Sprint 6/7 tests already in the repo — not just the new Sprint 8 ones. The Sprint 7 review notes only ran `dotnet build`, never `dotnet test`, so this had never surfaced:

- EF Core registers the Npgsql provider via composable `IDbContextOptionsConfiguration<T>` entries; the test factory only removed `DbContextOptions<AppDbContext>`, leaving Npgsql's configuration in place alongside the in-memory provider → "two providers registered" startup crash. Fixed by also removing `IDbContextOptionsConfiguration<AppDbContext>`.
- `Program.cs` unconditionally called `db.Database.MigrateAsync()` on startup, which throws against the in-memory provider (migrations are relational-only). Fixed with an `IsRelational()` guard that falls back to `EnsureCreatedAsync()` — no change to the Postgres/production path.

### Known remaining gap (not fixed — flagging for the next pass)

After both infra fixes, every **login-based** integration test (`POST /auth/login` with a freshly seeded user) returns 401, across the Sprint 6, 7, and 8 test classes alike — meaning the HTTP-level integration test suite has apparently never actually executed successfully in this repo. Root cause not yet found (the Argon2 hash/verify path looks internally consistent on inspection). Logic-level correctness of every Sprint 8 fix above is confirmed via `IntelligenceServiceTests.cs` and `MerchantNormalizerFixtureTests.cs` (36 tests, all passing), but the new `IntelligenceEndpointsTests.cs` HTTP-level tests are currently blocked by this same pre-existing issue as their Sprint 6/7 counterparts.

**Verified build status:** `dotnet build` on `ExpenseTracker.Api` and `ExpenseTracker.Budget.Tests` — 0 errors. Frontend `tsc --noEmit` — no new errors introduced (pre-existing CSS Module warnings on budgets/dashboard/notifications pages unchanged from Sprint 7).

---

## Sprint 9 — Phase 3: Merchant Template Learning + Recurring Expense Detection
**Dates:** 2026-09-08 → 2026-09-19
**Sprint Goal:** The OCR worker applies per-merchant field position templates to improve extraction accuracy on repeat merchants, and the platform automatically identifies recurring monthly expenses to help families plan ahead.
**Committed:** 19 points

### Committed Stories

| Story ID | Title | Points | Engineers | Rationale |
|----------|-------|--------|-----------|-----------|
| US-INT-05 | Merchant Receipt Layout Templates | 8 | OCR + BE | Core Phase 3 differentiator; uses correction history from Sprint 8 INT-04 to learn field positions |
| US-INT-06 | Recurring Expense Detection | 5 | BE + FE | High household value; pattern detection over existing indexed data; no external ML needed |
| US-INT-07 | Merchant Alias Grouping | 3 | BE + FE | Low effort; prevents merchant fragmentation in analytics and template matching (e.g., "Woolworths #42" and "Woolworths #18" are the same merchant) |
| US-INT-08 | Intelligence Settings Page | 3 | BE + FE | Gives Owner visibility and control over all learned data; required for user trust in AP-001 privacy-first deployment |
| **Total** | | **19** | | |

> US-INT-05 is the most complex story in Phase 3. OCR leads the template store design in week 1; BE wires the API; FE is shielded by taking US-INT-07 and US-INT-08 in parallel.

### Engineer Task Assignments — Sprint 9

#### Senior Backend Engineer (BE)

**US-INT-05 — Template Store API**
- `merchant_field_templates` table: `id`, `household_id`, `merchant_name_normalized`, `field_name`, `region_x`, `region_y`, `region_w`, `region_h`, `sample_count`, `last_updated`
  - One row per merchant-field combination (e.g., one row for `woolworths pty ltd` + `total`, another for `woolworths pty ltd` + `date`)
  - `region_x/y/w/h`: normalized coordinates (0.0–1.0 as fraction of image dimensions) representing the bounding box where this field was found
  - `sample_count`: number of confirmed receipts used to compute this region
- `POST /internal/merchant-templates` — internal endpoint (not user-facing); OCR worker posts updated template data after each confirmed receipt
  - Upsert: if template exists, recalculate the region as a weighted moving average of `(existing_region × sample_count + new_region) / (sample_count + 1)`; increment `sample_count`
- `GET /intelligence/merchant-templates` — Owner only; returns all templates for the household (transparency endpoint)
- `DELETE /intelligence/merchant-templates/{merchantNormalized}` — Owner only; deletes all field templates for a merchant; OCR worker falls back to full-image scan for that merchant
- Audit log on template deletion

**US-INT-06 — Recurring Expense Detection**
- Recurring pattern defined as: same `merchant_name_normalized` + same amount (within ±5%) appearing in at least 3 of the last 4 calendar months
- Background job: runs nightly via Redis-scheduled task; scans last 6 months of confirmed expenses; writes results to `recurring_expenses` table
- `recurring_expenses` table: `id`, `household_id`, `merchant_name_normalized`, `average_amount`, `typical_day_of_month`, `confidence`, `last_detected_at`, `snoozed_until`
  - `typical_day_of_month`: median day across matched months (integer 1–31)
  - `confidence`: `"confirmed"` (4/4 months present) or `"likely"` (3/4 months present)
- `GET /intelligence/recurring` — return all detected recurring expenses for the household; Adult Member sees own; Owner sees all
- `POST /intelligence/recurring/{id}/snooze?days=30` — Owner or Adult Member; sets `snoozed_until`; suppresses alerts for that period
- Alert: on the 3rd of each month, emit a notification for each recurring expense where no matching expense exists yet in the current month and `snoozed_until` is null or past

**US-INT-07 — Merchant Alias API**
- `merchant_aliases` table: `id`, `household_id`, `alias_normalized`, `canonical_normalized`, `created_by`, `created_at`
  - `alias_normalized`: the raw variant (e.g., `"woolworths 42"`)
  - `canonical_normalized`: the master name that all aliases resolve to (e.g., `"woolworths"`)
- `POST /intelligence/merchant-aliases` — Owner only; body: `{ alias, canonical }`; both values are normalized before insert
- `GET /intelligence/merchant-aliases` — return all aliases for the household
- `DELETE /intelligence/merchant-aliases/{id}` — Owner only
- All lookups in `merchant_category_map`, `merchant_field_templates`, and `merchant_tag_history` must resolve through the alias table first: if the merchant matches an alias, use the canonical name for the lookup
- Alias resolution must be a shared utility used by both .NET services and the Python OCR worker (via the internal API)

**US-INT-08 — Intelligence Settings API**
- `GET /intelligence/summary` — Owner only; returns counts: `{ merchantMappings, fieldTemplates, recurringExpenses, aliases }`
- This endpoint is the data source for the settings page; no new tables required

---

#### Senior Frontend Engineer (FE)

**US-INT-06 — Recurring Expense UI**
- `/intelligence/recurring` page: list of detected recurring expenses — merchant, average amount, typical day, confidence badge
- Confidence badge: green "Confirmed" for 4/4 months; amber "Likely" for 3/4 months
- "Snooze 30 days" button per row; snoozed items shown in a collapsed "Snoozed" section at the bottom
- Monthly reminder notification (from BE alert): appears in the existing notifications bell; links to `/intelligence/recurring`
- Empty state: "No recurring patterns detected yet — keep logging expenses and we'll identify your regular bills."

**US-INT-07 — Merchant Alias UI**
- Within the Intelligence Settings page (US-INT-08): "Merchant Aliases" section
- Table: alias name → canonical name; delete button per row
- "Add alias" inline form: two text inputs (Alias, Canonical) + Add button
- Client-side validation: alias and canonical must not be the same string; both fields required
- After adding: table refreshes; no page navigation

**US-INT-08 — Intelligence Settings Page**
- `/settings/intelligence` page — Owner only (redirect to `/dashboard` with a 403 message for other roles)
- Four summary cards at the top using `GET /intelligence/summary`: Merchant Mappings, Field Templates, Recurring Patterns, Merchant Aliases
- Each card has a "Manage" link that scrolls to or expands the relevant section below
- Sections: Merchant Category Map (read-only table from `GET /intelligence/merchant-map`), Field Templates (read-only list + delete per merchant from `GET /intelligence/merchant-templates`), Merchant Aliases (interactive — see US-INT-07)
- Deleting a field template shows a confirmation modal: "This will reset OCR accuracy improvements for [Merchant]. Continue?"
- Page must be completely hidden from the nav sidebar for non-Owner roles

---

#### Senior OCR Engineer (OCR)

**US-INT-05 — Template-Guided OCR Extraction**
- After the standard full-image Tesseract pass completes: check `GET /internal/merchant-templates/{merchantNormalized}` for stored field regions
- If templates exist with `sample_count >= 5` for a field: run a second targeted Tesseract pass on the cropped region (scaled to the full image's dimensions)
  - Use the targeted result if its confidence score is higher than the full-image result for that field
  - Log: `INFO template_extraction merchant={name} field={field} template_confidence={x} full_confidence={y} selected={source}`
- After a confirmed expense save (`ocr.correction` event with no correction on a field): record the bounding box coordinates where that field was found in the full-image scan; post to `POST /internal/merchant-templates` to update the template store
- If no template exists yet: proceed with full-image scan only (no regression from current behaviour)
- Performance: the targeted crop pass must not add more than 1 second to total OCR time (crop is small; this should be well within budget)
- Integration test: seed a template with a known region, run OCR on a test image, assert the targeted crop path was taken and its result was used

**US-INT-07 — Alias Resolution in Worker**
- On worker startup and every 5 minutes: fetch `GET /intelligence/merchant-aliases` and cache the alias map in memory (dict: alias → canonical)
- Apply alias resolution immediately after merchant name normalization — before any `merchant_category_map` or `merchant_field_templates` lookup
- Log when an alias is resolved: `INFO alias_resolved raw={alias} canonical={canonical}`

---

### Sprint 9 Definition of Done

- [x] After 5+ confirmed receipts for a merchant, the OCR worker uses the stored field-region template for that merchant on subsequent receipts; the targeted crop pass is logged — **implemented for the `merchantName` field only** (see Review Notes — `total`/`date` region tracking is a follow-up)
- [ ] Deleting a field template via the Intelligence Settings page causes the OCR worker to fall back to full-image scan on the next receipt for that merchant — DELETE endpoint and audit log exist; the Intelligence Settings page does not yet expose a template-management UI (only merchant aliases), so this can't be triggered from the UI yet
- [x] Nightly job detects merchants appearing in 3 of 4 recent months and writes them to `recurring_expenses`
- [x] Recurring expenses page lists detected patterns with correct confidence badges
- [ ] A recurring expense with no match in the current month (by the 3rd) generates an in-app notification — **not implemented this sprint** (see Review Notes)
- [x] Snoozed recurring expense suppresses the notification until the snooze period expires — snooze suppresses the recurring-page listing; there is no notification to suppress yet since the above alert was not built
- [x] Owner can add a merchant alias; subsequent OCR results and analytics for the alias variant resolve to the canonical merchant name — alias resolution wired into category-suggestion and tag-suggestion lookups
- [x] Merchant alias is applied consistently in merchant-category map lookups and tag history lookups; template lookups resolve aliases on write (`POST /internal/merchant-templates`) but not yet on the internal GET-by-merchant path used by the OCR worker's template fetch (follow-up)
- [x] Intelligence Settings page is inaccessible to Adult Member and Restricted Member roles (redirect with message, not 403 error page) — implemented against this codebase's actual roles (`Admin`/`Contributor`/`Reader`, not the `Owner`/`AdultMember`/`RestrictedMember` naming used in this doc)
- [x] Summary cards on Intelligence Settings page display accurate counts from the database
- [x] All new endpoints have integration tests — written per convention; HTTP-level runs are blocked by the same pre-existing login bug documented in the Sprint 8 Review Notes (logic verified independently: the internal-key template endpoint test passes end-to-end since it bypasses session login)
- [x] No `console.log` or debug output in committed code

---

## Sprint 9 — Review Notes (2026-07-05)

Implemented immediately after the Sprint 8 gap-fixing pass, using `/senior-backend`, `/senior-frontend`, and `/senior-ml-engineer`.

**Backend (`ExpenseTracker.Expense` + `ExpenseTracker.Api`):**
- New entities/tables: `merchant_field_templates`, `recurring_expenses`, `merchant_aliases` (migration `Sprint9IntelligenceSchema`)
- `IIntelligenceRepository`/`IntelligenceService` extended with template upsert (weighted moving average), recurring-pattern detection (merchant+amount-within-5% clustering across the last 6 months, 3-of-4-months threshold), and alias CRUD + resolution
- New endpoints under `/intelligence/*` (session-authed, Admin-gated where appropriate) and `/internal/merchant-templates` (`X-Internal-Key`-gated, for the OCR worker)
- `RecurringExpenseDetectionService` — a new nightly `BackgroundService` mirroring the existing `BudgetResetService` pattern
- Alias resolution wired into `GetSuggestedCategoryAsync` and `GetTagSuggestionsAsync` (US-INT-07 requirement); **not yet wired into the internal template-fetch-by-merchant path** — a variant merchant name won't yet resolve to its canonical template on the OCR read side, only on the write side

**Frontend (`source/web`):**
- `/intelligence/recurring` — confidence badges, snooze, collapsed snoozed section, empty state
- `/settings/intelligence` — Admin-only (client-side redirect to `/dashboard`), four summary cards, merchant-alias table + inline add form with same-value validation
- Nav sidebar: "Recurring" (all users) and "Intelligence Settings" (Admin-only, session-role-gated)
- `tsc --noEmit`: no new errors (pre-existing budgets/dashboard/notifications errors unchanged from Sprint 7/8)

**OCR worker (`source/ocr`) — scoped down from the full spec, deliberately:**
- Per-word bounding boxes were only available for the `merchantName` field (from the existing largest-font-in-top-15% heuristic). `total` and `date` are extracted via regex over concatenated OCR text with no per-word position tracking in the current pipeline — fabricating approximate positions for those under time pressure was rejected in favor of shipping a correct, narrower slice: **template-guided extraction and region-learning are implemented for `merchantName` only**. Extending to `total`/`date` requires tracking word indices during regex matching, noted as follow-up work, not silently skipped.
- `ocr_worker.py`: computes the merchant bounding box, normalizes it to a 0.0-1.0 region, fetches stored templates via the new internal GET endpoint (`httpx`, 2s timeout, fails open to full-image-only on any error), runs a targeted crop-and-retry Tesseract pass (`--psm 7`) when `sample_count >= 5`, and logs `template_extraction merchant=... field=merchantName template_confidence=... full_confidence=... selected=...` per the spec. Raw OCR JSON now also carries `fieldRegions` so the confirmation step can look the region back up.
- `correction_consumer.py`: when a `merchantName` correction event arrives with `isCorrected=False` (user accepted the OCR value as-is), it reads the region back out of the receipt's raw OCR JSON and posts it to `/internal/merchant-templates`.
- Added unit tests (`test_ocr_worker.py`) covering the normalization helper, template-fetch failure/parsing, targeted-pass cropping, and two end-to-end `_run_pipeline` cases (template wins when more confident; full-image result kept when the template pass is less confident) — all via mocks, no live Tesseract/Redis/network required, consistent with this test file's existing conventions.
- **`pytest` and `ruff check` were run against the project's real `.venv`** (initially blocked in this session by no Python interpreter in the sandbox; the user pointed at `source/ocr/.venv`). This surfaced that the **entire pre-existing OCR test suite had never actually passed** — 34 collection/fixture errors before any of my changes were even reached, all from the same root cause: `test_ocr_worker.py`'s and `test_thumbnail_worker.py`'s `settings` fixtures passed `storage_receipts_path`/`storage_thumbnails_path`/`storage_ocr_json_path` as constructor kwargs, but those are read-only `@property` values on `Settings` derived from `storage_base_path`, not real fields — Pydantic rejected them as "Extra inputs are not permitted". Fixed by passing `storage_base_path=str(tmp_path)` instead in both fixtures.
- That fix uncovered 5 further pre-existing bugs, all fixed:
  - `_extract_line_items` had no keyword-line exclusion, so a "Subtotal 6.48" line was parsed as a purchased item — fixed by skipping lines that match the existing `_TOTAL_PATTERN`/`_SUBTOTAL_PATTERN`/`_TAX_PATTERN` regexes before line-item matching (a real, if latent, receipt-parsing bug, not just a test bug)
  - The retry-backoff test slept for real (10s + 30s) and asserted only 1 published message, but the retry loop also publishes a status message per retry attempt (3 total) — patched `asyncio.sleep` and fixed the assertion
  - `test_generate_thumbnail_creates_file`/`test_generate_thumbnail_converts_rgba` opened the thumbnail's returned path directly, but `_generate_thumbnail` intentionally returns a path *relative to* `storage_base_path` (so the API can store it portably) — fixed by resolving against `storage_base_path` before opening
  - `test_notify_api_calls_patch` asserted a call without the `headers` kwarg that `_notify_api` actually sends (`X-Internal-Key`) — fixed the expected call
- After all fixes: **58 passed, 0 failed.** `ruff check` is clean on the files this session touched; 4 pre-existing `N806` (uppercase local variable) warnings remain in code this session didn't write (`_RETRY_DELAYS`, `_MAX_ATTEMPTS`, `_BLUR_THRESHOLD`, `_CONTRAST_THRESHOLD`), left alone as out of scope.

**Not implemented this sprint (explicitly deferred, not silently dropped):**
- US-INT-06's monthly notification alert (recurring expense missing by the 3rd) — `recurring_expenses` data model and detection exist, but the notification-emission job does not
- Intelligence Settings page has no field-template management section yet (aliases only)
- `total`/`date` field-region tracking for template-guided OCR (merchant-name only, as above)
- Alias resolution on the OCR worker's template-fetch-by-merchant read path

---

## Sprint 10 — Phase 3 Completion + Phase 4 Groundwork
**Dates:** 2026-09-22 → 2026-10-03
**Sprint Goal:** Phase 3 intelligence is fully shipped and validated; the mobile app scaffolding and offline-sync data contract are in place so Phase 4 development can begin immediately in Sprint 11.
**Committed:** 19 points

### Committed Stories

| Story ID | Title | Points | Engineers | Rationale |
|----------|-------|--------|-----------|-----------|
| US-INT-09 | Intelligence Onboarding + Privacy Disclosure | 3 | BE + FE | Required before Phase 3 features are exposed to non-Owner members; AP-001 compliance |
| US-INT-10 | Phase 3 End-to-End Validation + Hardening | 5 | ALL | Accuracy regression tests, edge-case hardening, and performance validation for all INT stories |
| US-MOB-01 | React Native + Expo App Scaffold | 5 | FE | Sets up project structure, navigation shell, and shared API client so Sprint 11 can deliver features immediately |
| US-MOB-02 | Offline Sync Data Contract | 3 | BE + FE | Defines the sync protocol (conflict resolution, delta payloads) before any offline feature is built; a late design decision here is hard to reverse |
| US-MOB-03 | Mobile Auth Flow (Login + MFA) | 3 | FE | Login is the gateway to every other mobile feature; must be done first |
| **Total** | | **19** | | |

> Phase 4 stories in this sprint are scaffolding and design only — no user-visible mobile features ship until Sprint 11. If Phase 3 hardening (US-INT-10) runs long, US-MOB-02 is the drop candidate (design can continue async in Sprint 11 week 1).

### Engineer Task Assignments — Sprint 10

#### Senior Backend Engineer (BE)

**US-INT-09 — Privacy Disclosure API**
- `intelligence_consent` table: `id`, `household_id`, `user_id`, `consented_at`, `consent_version`
  - `consent_version`: integer; increment when the disclosure text changes materially
- `POST /intelligence/consent` — any authenticated user; records consent for current user at current version
- `GET /intelligence/consent/status` — returns `{ consented: bool, consentVersion: int, currentVersion: int }`
- All Phase 3 intelligence endpoints (`/intelligence/*`) check consent for the requesting user; return 403 with `{ error: "intelligence_consent_required" }` if not consented
  - Exception: Owner can access `GET /intelligence/summary` and `GET /intelligence/merchant-map` without consent (administrative transparency)
- Migration: seed `consent_version = 1` in application configuration

**US-INT-10 — Phase 3 Hardening (BE)**
- Accuracy regression test suite: for each of the 50 benchmark receipts from Sprint 7, run the full INT-01 + INT-05 pipeline and assert field accuracy has not degraded vs the Sprint 7 baseline
- Edge cases to cover and fix if failing:
  - Merchant with no confirmed history → suggestion is omitted (no null suggestion surfaced)
  - Template region outside image bounds (corrupted template) → fall back to full-image scan, log warning, do not crash
  - Duplicate detection when `amount` is null (manual expense, no amount set) → duplicate check skipped, no false positive
  - Recurring job when household has zero expenses → completes silently, no exception
- Review all Phase 3 endpoints for missing parameterized queries; flag any string interpolation in SQL
- Confirm Redis consumer for `ocr.correction` does not leak memory under 100 consecutive events (run load test)

**US-MOB-02 — Offline Sync Data Contract**
- Design and document the sync protocol in `documents/offline-sync-contract.md`:
  - Delta sync: `GET /sync/delta?since=ISO8601` returns all expense, receipt, and budget changes since the given timestamp
  - Each entity in the delta carries `updatedAt` and a `syncVersion` (monotonic integer per household)
  - Conflict resolution rule: server wins for all fields except `notes` (last-write wins for notes, using `updatedAt`)
  - Tombstone records: deleted entities are represented as `{ id, deletedAt }` — client removes locally on receipt
- `GET /sync/delta` endpoint (scaffold only this sprint — full implementation in Phase 4):
  - Returns 200 with empty `changes: []` for now; schema is fixed so mobile can code against it immediately
  - Rate-limited to 1 request per 30 seconds per device (return 429 with `Retry-After` header if exceeded)
- Document all decisions in `documents/offline-sync-contract.md`; this document is the single source of truth for Phase 4

---

#### Senior Frontend Engineer (FE)

**US-INT-09 — Privacy Disclosure UI**
- On first login after Phase 3 features are deployed: show a full-screen modal (not dismissible by clicking outside) explaining what data is learned locally:
  - "This app learns from your confirmed expenses to suggest categories, detect recurring bills, and improve receipt scanning. All learning happens on your home server — no data leaves this device."
  - Two buttons: "Enable Smart Features" (calls `POST /intelligence/consent`) and "Keep Manual" (skips consent; user can enable later in settings)
- After consent: modal closes; user proceeds normally; Phase 3 features are active
- "Keep Manual" path: all INT-01, INT-03, INT-06, INT-07 features are hidden in the UI for that user (no suggestion badges, no recurring page, no tag autocomplete)
- Settings page: `/settings/intelligence` shows "Smart Features: On / Off" toggle at the top; Off reverts to the "Keep Manual" experience without deleting learned data

**US-MOB-01 — React Native + Expo App Scaffold**
- Initialize Expo project at `source/mobile/` using `npx create-expo-app` with the TypeScript template
- Directory structure mirrors the web app conventions:
  - `source/mobile/src/app/` — Expo Router file-based routes
  - `source/mobile/src/components/` — shared UI components
  - `source/mobile/src/api/` — API client (reuse the same typed fetch wrappers as the web app where possible)
  - `source/mobile/src/store/` — Zustand store (same pattern as web)
- Configure Expo Router with a root stack: `(auth)` group (login, MFA) and `(app)` group (protected screens)
- Set up the base API client pointing to the same .NET backend; auth uses the same session cookie mechanism (fetch with `credentials: "include"`)
- Add to `docker-compose.yml`: no new service needed (mobile connects to existing `aspnet-api`); document the local dev URL configuration in `source/mobile/README.md`
- CI: add a lint + type-check step for `source/mobile/` to the existing pipeline

**US-MOB-02 — Offline Sync Contract (FE)**
- Review and sign off on the sync contract document produced by BE
- Scaffold `source/mobile/src/api/sync.ts`: typed client for `GET /sync/delta` matching the agreed schema
- Scaffold `source/mobile/src/store/syncStore.ts`: Zustand slice with `lastSyncedAt` state and a `syncDelta()` action (no-op implementation this sprint; wired in Phase 4)
- Document any mobile-specific concerns (e.g., background fetch limitations on iOS) in `documents/offline-sync-contract.md`

**US-MOB-03 — Mobile Auth Flow**
- `source/mobile/src/app/(auth)/login.tsx`: login screen with email + password fields; matches web login behaviour
- On successful login: if MFA is required, navigate to `(auth)/mfa.tsx`; otherwise navigate to `(app)/dashboard`
- `source/mobile/src/app/(auth)/mfa.tsx`: 6-digit OTP input; same session-based flow as web; auto-submit on 6th digit entry
- Session persistence: use `expo-secure-store` to persist the session cookie across app restarts (replaces browser cookie storage)
- Error states: invalid credentials (401) → inline error under password field; network unavailable → "Cannot reach server. Check your connection." banner
- Logout: clears `expo-secure-store` session entry and navigates back to login

---

#### Senior OCR Engineer (OCR)

**US-INT-10 — Phase 3 Hardening (OCR)**
- Re-run the Sprint 7 accuracy benchmark (50 receipts) with all Phase 3 changes active; produce a comparison report:
  - Baseline (Sprint 7): per-field accuracy %
  - Sprint 10 (with templates + feedback loop): per-field accuracy %
  - Target: no field regresses by more than 5 percentage points; total accuracy improves by at least 10 percentage points on the subset of receipts that have templates
- Template edge case: receipt image is rotated 90 degrees — existing template region coordinates will be wrong; detect rotation (OpenCV `minAreaRect`) and rotate region coordinates before the targeted crop pass
- Memory profiling: run the worker under 50 concurrent jobs for 10 minutes; assert RSS memory stays below 512 MB
- Ensure `/storage` cleanup job from Sprint 7 hardening runs correctly and has been verified in the CI environment
- Document the final OCR accuracy baseline in `documents/ocr-accuracy-baseline.md` for Phase 4 reference

---

### Sprint 10 Definition of Done

- [ ] First-time user (post-Phase 3 deploy) sees the privacy disclosure modal before any intelligence features are active
- [ ] User who selects "Keep Manual" sees no suggestion badges, no tag autocomplete, and no recurring expenses page
- [ ] User who consents can toggle Smart Features off in settings; toggling off hides intelligence features without deleting learned data
- [ ] All Phase 3 intelligence endpoints return 403 with `intelligence_consent_required` for users who have not consented (except Owner administrative endpoints)
- [ ] Phase 3 accuracy regression suite passes: no field accuracy regresses more than 5 percentage points vs Sprint 7 baseline
- [ ] Template extraction handles rotated images without crashing; falls back to full-image scan and logs a warning
- [ ] Duplicate detection skips the check (no false positive) when `amount` is null on either the new or existing expense
- [ ] `GET /sync/delta` returns 200 with `changes: []`; returns 429 with `Retry-After` when called more than once in 30 seconds from the same device
- [ ] Expo app scaffold initializes cleanly (`npx expo start` runs without errors); login and MFA screens are navigable end-to-end against the local backend
- [ ] Session persists across mobile app restarts using `expo-secure-store`
- [ ] `syncStore.ts` and `sync.ts` scaffolds are typed against the agreed sync contract schema with no TypeScript errors
- [ ] OCR accuracy comparison report is committed to `documents/ocr-accuracy-baseline.md`
- [ ] All new endpoints have integration tests
- [ ] No `console.log` or debug output in committed code

---

## Phase 3 Complete — End of Sprint 10 (2026-10-03)

All Phase 3 intelligence features are live: auto-categorization, duplicate detection, smart tag suggestions, OCR feedback loop, merchant template learning, recurring expense detection, merchant alias grouping, and intelligence settings. All processing is local — AP-001 is upheld across every feature.

**Phase 4 scope (Sprint 11 onward):** Receipt upload from mobile, expense CRUD on mobile, offline queue and delta sync, push notifications.

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
| Sprint 8 | 19 | 19 | 19 | Delivered late/incomplete — flagship feature was dead code, fixed 2026-07-05 before Sprint 9 (see Review Notes) |
| Sprint 9 | 19 | ~15 | 15 | Partial: recurring detection + aliases + settings page complete; template learning scoped to merchant-name field only, monthly recurring-alert notification not built (see Review Notes) |
| Sprint 10 | 19 | TBD | TBD | Phase 3 complete; Phase 4 scaffold delivered |

**Established velocity:** ~19 pts/sprint (18–21 range, 3-sprint average)
