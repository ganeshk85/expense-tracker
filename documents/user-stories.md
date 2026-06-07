# User Stories & Acceptance Criteria
**Project:** Expense Tracker — Family Expense Intelligence Platform
**Role:** Product Owner
**Source:** Privacy-First-Expense-Tracker-Architecture-And-Brd.docx
**Created:** 2026-05-19

---

## Roles Reference

| Role | Description |
|------|-------------|
| **Owner** | Manages users, budgets, all expenses, settings, and audit logs |
| **Adult Member** | Uploads receipts, manages own expenses, views shared budgets |
| **Restricted Member** | Uploads receipts, views only assigned expenses |

## Phase Reference

| Phase | Scope |
|-------|-------|
| **Phase 1** | Foundation — auth, receipt upload, OCR, expense management |
| **Phase 2** | Budgeting & Search — budgets, analytics, alerts |
| **Phase 3** | Intelligence — merchant template learning, advanced parsing |
| **Phase 4** | Mobile — React Native, offline sync |

---

## Epic 1: Authentication & Security (FR-AUTH)

> Enable secure, role-aware access to the platform with full audit visibility.

**Total Epic Points:** 21

---

### US-AUTH-01 — User Login
**Phase:** 1 | **Points:** 3 | **Priority:** Critical | **Type:** Feature

**Story:**
As an **Owner** or **Adult Member**,
I want to log in with a username and password,
So that I can securely access my family's expense data.

**Acceptance Criteria:**
- Given valid credentials are entered, when I submit the login form, then I am authenticated and redirected to the dashboard within 2 seconds.
- Given an incorrect password is entered, when I submit the form, then an error message is shown without revealing which field is wrong.
- Given 5 consecutive failed login attempts, when another attempt is made, then the account is temporarily locked and a message is displayed.
- Given a successful login, when the session is created, then it uses an HTTP-only cookie with a secure flag and a 30-minute idle timeout.
- Given I close the browser, when I reopen it and navigate to the app, then I must log in again (no persistent session by default).

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

### US-AUTH-02 — Multi-Factor Authentication (MFA)
**Phase:** 1 | **Points:** 5 | **Priority:** High | **Type:** Feature

**Story:**
As an **Owner**,
I want to enable MFA for any user account,
So that we have an additional layer of security against unauthorized access.

**Acceptance Criteria:**
- Given MFA is enabled for my account, when I log in with valid credentials, then I am prompted for a one-time code before gaining access.
- Given I enter a valid OTP code, when I submit, then I am authenticated and redirected to the dashboard.
- Given I enter an invalid OTP code, when I submit, then I see an error and the login is rejected.
- Given an Owner, when they access user settings, then they can enable or disable MFA for any account in the household.
- Given MFA setup, when initiated, then I am shown a QR code to scan with an authenticator app.

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

### US-AUTH-03 — Role-Based Access Control
**Phase:** 1 | **Points:** 5 | **Priority:** Critical | **Type:** Feature

**Story:**
As an **Owner**,
I want to assign roles to household members,
So that each person only accesses what is appropriate for their level of trust.

**Acceptance Criteria:**
- Given I am an Owner, when I invite a new member, then I can assign them the role of Owner, Adult Member, or Restricted Member.
- Given a Restricted Member is logged in, when they attempt to view another member's expenses, then they receive a 403 Forbidden response.
- Given an Adult Member is logged in, when they attempt to access settings or audit logs, then they are redirected with an "Access Denied" message.
- Given a role is changed for a user, when they next make a request, then the new permissions apply immediately (no re-login required).
- Given all protected API routes, when an unauthenticated request is made, then a 401 Unauthorized response is returned.

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

### US-AUTH-04 — User Invitation & Account Setup
**Phase:** 1 | **Points:** 3 | **Priority:** High | **Type:** Feature

**Story:**
As an **Owner**,
I want to invite family members to the platform,
So that each person has their own secure account without self-registering.

**Acceptance Criteria:**
- Given I am an Owner, when I enter a family member's name and assign a role, then an invite link or temporary password is generated.
- Given a new member uses the invite link, when they set their password, then the account is activated and they are logged in.
- Given an invite link is generated, when 48 hours pass without use, then the link expires and a new one must be issued.
- Given a member account is created, when viewed in settings, then it shows their role, last login, and account status.

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

### US-AUTH-05 — Audit Logging
**Phase:** 1 | **Points:** 5 | **Priority:** High | **Type:** Feature

**Story:**
As an **Owner**,
I want a tamper-evident audit log of all significant system actions,
So that I can review who did what and when across the household account.

**Acceptance Criteria:**
- Given any user logs in or out, when the event occurs, then it is recorded with timestamp, user ID, and IP address.
- Given an expense, receipt, or budget is created, edited, or deleted, when the action is performed, then it is recorded in the audit log with the before/after values.
- Given I am an Owner, when I access the audit log page, then I can filter by date range, user, and action type.
- Given a Restricted Member or Adult Member, when they attempt to access audit logs, then they receive an Access Denied response.
- Given the audit log, when entries are written, then they cannot be edited or deleted by any user role including Owner.

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

## Epic 2: Receipt Upload (FR-REC)

> Enable fast, flexible receipt capture from any device or source.

**Total Epic Points:** 18

---

### US-REC-01 — Upload Receipt via File or Drag-and-Drop
**Phase:** 1 | **Points:** 3 | **Priority:** Critical | **Type:** Feature

**Story:**
As an **Adult Member**,
I want to upload a receipt image or PDF by selecting a file or dragging it into the browser,
So that I can capture expense records quickly from my desktop.

**Acceptance Criteria:**
- Given I am on the receipt upload page, when I drag and drop a JPG, PNG, HEIC, or PDF file, then the file is accepted and an upload progress indicator is shown.
- Given I click "Select File," when I choose a supported file, then the upload begins and completes within 2 seconds for files under 10 MB.
- Given I upload an unsupported file type (e.g., .xlsx), when the upload is attempted, then an inline error message states the accepted formats.
- Given the upload completes successfully, when the page updates, then a thumbnail preview of the receipt is shown.
- Given the original file, when stored, then it is preserved exactly as uploaded (no lossy compression of the original).

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

### US-REC-02 — Upload Receipt via Mobile Camera
**Phase:** 1 | **Points:** 5 | **Priority:** High | **Type:** Feature

**Story:**
As an **Adult Member** using a mobile browser,
I want to take a photo of a receipt with my camera and upload it immediately,
So that I can capture receipts on the go without switching devices.

**Acceptance Criteria:**
- Given I am on the upload page on a mobile device, when I tap "Capture Photo," then the device camera is activated.
- Given I take a photo, when confirmed, then the image is uploaded and a thumbnail preview appears within 2 seconds.
- Given I am on mobile, when I tap "Choose from Gallery," then I can select an existing image from my photo library.
- Given a photo is taken in poor lighting, when the image quality is assessed, then a warning banner appears suggesting a retake.
- Given the image is captured, when processed, then EXIF data (GPS coordinates, device info) is stripped before storage.

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

### US-REC-03 — Attach Multiple Receipts to One Expense
**Phase:** 1 | **Points:** 3 | **Priority:** High | **Type:** Feature

**Story:**
As an **Adult Member**,
I want to attach multiple receipt images to a single expense,
So that I can handle split receipts or multi-page documents without creating duplicate entries.

**Acceptance Criteria:**
- Given I am creating or editing an expense, when I upload a second receipt, then it is appended to the expense alongside the first.
- Given multiple receipts are attached, when I view the expense detail, then all thumbnails are displayed in a scrollable gallery.
- Given I have multiple receipts, when I remove one, then the remaining receipts stay intact and the expense is saved without the deleted image.
- Given an expense with multiple receipts, when the OCR runs, then each receipt is processed independently and results are merged into the expense.

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

### US-REC-04 — Image Quality Detection
**Phase:** 1 | **Points:** 3 | **Priority:** Medium | **Type:** Feature

**Story:**
As an **Adult Member**,
I want to be warned if my uploaded receipt image is too blurry or dark to read,
So that I can re-upload a better image before OCR fails.

**Acceptance Criteria:**
- Given I upload a blurry image (below a threshold blur score), when the quality check runs, then a warning banner appears: "This image may be hard to read. Consider retaking it."
- Given I upload a well-lit, sharp image, when the quality check runs, then no warning is shown and OCR proceeds automatically.
- Given a quality warning is shown, when I dismiss it and proceed, then OCR is still attempted on the original image.
- Given quality detection runs, when complete, then it does not add more than 500ms to the upload response time.

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

### US-REC-05 — Automatic Thumbnail Generation
**Phase:** 1 | **Points:** 2 | **Priority:** Medium | **Type:** Enabler

**Story:**
As a **Developer**,
I need thumbnails generated automatically on receipt upload,
So that the UI can display receipt previews without loading full-resolution files.

**Acceptance Criteria:**
- Given any receipt is uploaded, when stored, then a thumbnail (max 300×400px) is generated and saved to `/storage/thumbnails/`.
- Given a PDF receipt, when a thumbnail is generated, then the first page is used.
- Given the original file is a HEIC, when the thumbnail is generated, then it is converted to JPEG format.
- Given a thumbnail is requested, when served, then it responds within 200ms from local storage.

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

### US-REC-06 — Restricted Member Receipt Upload
**Phase:** 1 | **Points:** 2 | **Priority:** Medium | **Type:** Feature

**Story:**
As a **Restricted Member**,
I want to upload a receipt and submit it for review,
So that I can contribute expense records even with limited account access.

**Acceptance Criteria:**
- Given I am a Restricted Member, when I upload a receipt, then it is accepted and queued for OCR like any other upload.
- Given my upload is processed, when the expense is created, then it is visible to me in my assigned expenses view only.
- Given I am a Restricted Member, when I attempt to view another member's uploaded receipts, then I receive an Access Denied response.
- Given my upload is submitted, when an Owner or Adult Member reviews it, then they can assign it to a shared expense or approve it.

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

## Epic 3: OCR & Receipt Extraction (FR-OCR)

> Extract structured expense data from receipts automatically using local OCR processing.

**Total Epic Points:** 24

---

### US-OCR-01 — Automatic Receipt Data Extraction
**Phase:** 1 | **Points:** 8 | **Priority:** Critical | **Type:** Feature

**Story:**
As an **Adult Member**,
I want the system to automatically extract key data from my uploaded receipt,
So that I don't have to type in merchant names, totals, and dates manually.

**Acceptance Criteria:**
- Given a receipt is uploaded, when OCR processing completes (within 8 seconds), then the following fields are extracted where present: merchant name, address, date, time, subtotal, tax, total, and line items (name, quantity, unit price).
- Given extraction is complete, when I view the expense, then extracted fields are pre-populated in the edit form.
- Given OCR processes the receipt, when complete, then the raw OCR JSON is stored at `/storage/ocr-json/` and never deleted.
- Given OCR fails entirely, when I view the expense, then a message states "OCR could not read this receipt — please enter details manually" and no partial data is incorrectly saved.
- Given the receipt is in a non-English locale, when OCR runs, then the system attempts extraction and falls back to raw text if structured parsing fails.
- Given OCR is processing, when I refresh the page, then a status indicator shows "Processing..." until complete.

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

### US-OCR-02 — Confidence Scoring Display
**Phase:** 1 | **Points:** 3 | **Priority:** High | **Type:** Feature

**Story:**
As an **Adult Member**,
I want to see a confidence score for each extracted field,
So that I know which values need manual review before saving.

**Acceptance Criteria:**
- Given OCR extraction is complete, when I view the extracted data, then each field shows a confidence indicator (e.g., High / Medium / Low or a percentage).
- Given a field has low confidence (< 70%), when displayed, then it is visually highlighted in amber to prompt review.
- Given a field has high confidence (≥ 90%), when displayed, then it appears without a warning indicator.
- Given all fields have been reviewed and saved, when viewing the expense, then confidence scores are no longer shown (they served their purpose at review time).

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

### US-OCR-03 — Manual Correction of Extracted Data
**Phase:** 1 | **Points:** 5 | **Priority:** Critical | **Type:** Feature

**Story:**
As an **Adult Member**,
I want to correct any incorrectly extracted field before saving the expense,
So that my records are accurate even when OCR makes mistakes.

**Acceptance Criteria:**
- Given OCR extraction is complete, when I open the review form, then all extracted fields are editable inline.
- Given I change a field value, when I save, then the corrected value is persisted as the final expense data.
- Given I correct a field, when saved, then the correction is logged alongside the original OCR value for audit purposes.
- Given I edit the total amount, when the line items are shown, then the system alerts me if items no longer sum to the new total.
- Given all corrections are made, when I click "Confirm Expense," then the expense is saved and I am redirected to the expense list.

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

### US-OCR-04 — Barcode & QR Code Parsing
**Phase:** 1 | **Points:** 5 | **Priority:** Medium | **Type:** Feature

**Story:**
As an **Adult Member**,
I want barcodes and QR codes on receipts to be automatically decoded,
So that loyalty cards, product identifiers, or payment references are captured without manual entry.

**Acceptance Criteria:**
- Given a receipt with a scannable barcode (1D or 2D), when OCR processing runs, then the decoded value is extracted and stored on the receipt record.
- Given a QR code is present on the receipt, when decoded, then the content (URL, text, or structured data) is displayed in the receipt detail view.
- Given no barcode or QR code is detected, when OCR completes, then no barcode field is shown (field is hidden, not empty).
- Given parsing runs, when complete, then barcode decoding does not extend total OCR time beyond the 8-second target.

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

### US-OCR-05 — OCR Retry on Failure
**Phase:** 1 | **Points:** 3 | **Priority:** Medium | **Type:** Feature

**Story:**
As an **Adult Member**,
I want failed OCR jobs to retry automatically,
So that transient processing errors don't require me to re-upload the receipt.

**Acceptance Criteria:**
- Given an OCR job fails due to a worker error, when the failure is detected, then the job is automatically retried up to 3 times with exponential backoff.
- Given all 3 retries fail, when the final failure occurs, then the receipt status is marked "OCR Failed" and I am notified in the UI.
- Given an OCR job is retrying, when I check the receipt status, then a "Processing (retry X of 3)..." indicator is shown.
- Given a retry succeeds, when complete, then the expense is updated normally as if it had succeeded on the first attempt.

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

## Epic 4: Expense Management (FR-EXP)

> Create, organize, and manage expense records from receipts or manual entry.

**Total Epic Points:** 21

---

### US-EXP-01 — Create Expense Manually
**Phase:** 1 | **Points:** 3 | **Priority:** Critical | **Type:** Feature

**Story:**
As an **Adult Member**,
I want to create an expense manually without a receipt,
So that I can record cash payments or expenses where no receipt was issued.

**Acceptance Criteria:**
- Given I navigate to "New Expense," when I fill in merchant, date, amount, and category and submit, then the expense is saved and appears in my expense list.
- Given I submit the form with the amount field empty, when validation runs, then an inline error states "Amount is required."
- Given I create a manual expense, when saved, then the source is tagged as "Manual" and distinguishable from OCR-sourced entries.
- Given I create an expense, when viewing the list, then it appears immediately without page refresh.
- Given no receipt is attached, when the expense is saved, then no receipt preview area is shown on the detail page.

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

### US-EXP-02 — Categorize and Tag an Expense
**Phase:** 1 | **Points:** 3 | **Priority:** High | **Type:** Feature

**Story:**
As an **Adult Member**,
I want to assign a category and optional tags to each expense,
So that I can organize spending for budgeting and search purposes.

**Acceptance Criteria:**
- Given I am on the expense edit form, when I open the category dropdown, then I see the predefined list of categories (e.g., Groceries, Dining, Utilities, Transport, Health, Other).
- Given I select a category, when saved, then it appears on the expense card in the list view.
- Given I type a new tag name, when I press Enter or comma, then the tag is added to the expense.
- Given OCR suggests a category based on the merchant name, when the form loads, then the suggested category is pre-selected and I can override it.
- Given I remove a tag, when saved, then the tag no longer appears on the expense but other expenses with the same tag are not affected.

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

### US-EXP-03 — Add Notes and Attachments to an Expense
**Phase:** 1 | **Points:** 2 | **Priority:** Medium | **Type:** Feature

**Story:**
As an **Adult Member**,
I want to add a note and optional file attachments to any expense,
So that I can record context (e.g., "business lunch with client") or attach a warranty document.

**Acceptance Criteria:**
- Given I am on the expense edit form, when I type in the Notes field and save, then the note is displayed on the expense detail page.
- Given I attach a file (any format up to 10 MB), when saved, then a download link appears on the expense detail.
- Given I attach a file that exceeds 10 MB, when attempted, then an error message states the file size limit.
- Given I have added a note, when I clear it and save, then the expense is saved with no note.

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

### US-EXP-04 — Mark an Expense as Shared
**Phase:** 1 | **Points:** 5 | **Priority:** High | **Type:** Feature

**Story:**
As an **Adult Member**,
I want to mark an expense as shared and split it among household members,
So that everyone can see their contribution to shared household spending.

**Acceptance Criteria:**
- Given I am editing an expense, when I toggle "Shared Expense," then I can select which household members share it and assign split amounts or percentages.
- Given a shared expense is saved, when a selected member logs in, then the shared expense appears in their expense list with their portion amount.
- Given a Restricted Member is excluded from a shared expense, when they view expenses, then they do not see the shared expense at all.
- Given I am an Owner, when I view any shared expense, then I see all members' shares and the full total.
- Given a shared expense total is edited, when I save, then the member shares are recalculated and members are shown an updated view.

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

### US-EXP-05 — View and Edit Expense History
**Phase:** 1 | **Points:** 3 | **Priority:** High | **Type:** Feature

**Story:**
As an **Adult Member**,
I want to view a list of all my expenses and open any one for editing,
So that I can review and correct past entries.

**Acceptance Criteria:**
- Given I navigate to the expense list, when the page loads, then my expenses appear sorted by date (most recent first) within 3 seconds.
- Given I click an expense row, when the detail page opens, then all fields are editable.
- Given I edit and save an expense, when I return to the list, then the updated values are reflected immediately.
- Given I am an Owner, when I view the expense list, then I can toggle between "My Expenses" and "All Household Expenses."
- Given I delete an expense, when confirmed, then it is removed from the list and an audit log entry is created.

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

### US-EXP-06 — Item-Level Expense Breakdown
**Phase:** 1 | **Points:** 5 | **Priority:** Medium | **Type:** Feature

**Story:**
As an **Adult Member**,
I want to view and edit individual line items extracted from a receipt,
So that I can track spending at the product level, not just the receipt total.

**Acceptance Criteria:**
- Given OCR extracted line items, when I view the expense detail, then each item is listed with its name, quantity, and unit price.
- Given I edit an item name or price, when saved, then the updated item values are persisted.
- Given I add a new line item manually, when saved, then it appears in the item list.
- Given I delete a line item, when saved, then the expense total is recalculated automatically.
- Given line items do not sum to the total, when I attempt to save, then a warning is shown: "Line items total does not match receipt total."

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

## Epic 5: Budgeting (FR-BUD)

> Set and track spending budgets at the category and household level.

**Total Epic Points:** 16

---

### US-BUD-01 — Set Monthly Category Budget
**Phase:** 2 | **Points:** 3 | **Priority:** High | **Type:** Feature

**Story:**
As an **Owner**,
I want to set a monthly spending budget for each expense category,
So that the household knows how much we can spend per category each month.

**Acceptance Criteria:**
- Given I navigate to Budget Settings, when I select a category and enter a monthly limit, then the budget is saved and becomes active from the current month.
- Given a budget is set for Groceries at $500, when I view the Groceries budget card, then I see "$X spent of $500" with a progress bar.
- Given I set a budget to $0, when the form is submitted, then a validation error states "Budget must be greater than zero."
- Given I update an existing budget mid-month, when saved, then the progress is recalculated against the new limit immediately.

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

### US-BUD-02 — Household Shared Budget
**Phase:** 2 | **Points:** 5 | **Priority:** High | **Type:** Feature

**Story:**
As an **Owner**,
I want to set a shared household budget visible to all Adult Members,
So that the whole family can track collective spending toward a shared financial goal.

**Acceptance Criteria:**
- Given I create a shared household budget, when saved, then all Adult Members can see the budget and their contribution to it.
- Given a shared budget is active, when any member adds an expense in that category, then the shared budget progress updates in real time.
- Given I am a Restricted Member, when I access shared budget details, then I can see only total progress, not individual member breakdowns.
- Given I am an Owner, when I view the shared budget, then I see a breakdown of each member's contribution.
- Given a shared budget exists, when I delete it, then members are notified via an in-app message that the budget was removed.

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

### US-BUD-03 — Budget Threshold Alerts
**Phase:** 2 | **Points:** 5 | **Priority:** Medium | **Type:** Feature

**Story:**
As an **Owner**,
I want to receive an alert when a budget category reaches a set spending threshold,
So that I can take action before the budget is exceeded.

**Acceptance Criteria:**
- Given I configure a budget alert at 80% of a category limit, when cumulative spending crosses 80%, then an in-app notification is shown.
- Given the budget limit is fully reached (100%), when an expense is added that exceeds it, then an alert states "Groceries budget exceeded."
- Given an alert fires, when I dismiss it, then it does not reappear until the next monthly cycle resets.
- Given no alert threshold is configured, when spending increases, then no alert is generated for that category.
- Given alerts are triggered, when I navigate to Notifications, then all recent budget alerts are listed with the date and category.

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

### US-BUD-04 — Monthly Budget Reset
**Phase:** 2 | **Points:** 3 | **Priority:** Medium | **Type:** Enabler

**Story:**
As a **Developer**,
I need budgets to reset automatically at the start of each calendar month,
So that spending progress reflects only the current month without manual intervention.

**Acceptance Criteria:**
- Given a new calendar month begins, when the system processes the reset, then all budget progress counters reset to $0 spent.
- Given the reset runs, when completed, then historical monthly summaries remain accessible and are not overwritten.
- Given the reset runs at midnight on the 1st, when the user opens the app that morning, then the current month shows fresh progress.
- Given the reset process fails, when detected, then an error is logged and a retry is triggered automatically.

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

## Epic 6: Search & Analytics (FR-SRCH)

> Enable fast, flexible search across all expense records and surface meaningful spending insights.

**Total Epic Points:** 21

---

### US-SRCH-01 — Multi-Field Expense Search
**Phase:** 2 | **Points:** 5 | **Priority:** High | **Type:** Feature

**Story:**
As an **Adult Member**,
I want to search my expenses by merchant, item, amount, category, barcode, or date range,
So that I can quickly find a specific receipt or group of transactions.

**Acceptance Criteria:**
- Given I type in the search bar, when results are returned, then they appear within 1 second.
- Given I search by merchant name, when results are shown, then only expenses with a matching merchant name are returned.
- Given I set a date range filter, when applied, then only expenses within that date range are shown.
- Given I combine filters (e.g., merchant + date range), when applied, then only expenses matching all filters are returned.
- Given my search returns no results, when the list is shown, then a message states "No expenses found for your search."
- Given I am a Restricted Member, when I search, then results include only my assigned expenses.

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

### US-SRCH-02 — Spending Summary Dashboard
**Phase:** 2 | **Points:** 5 | **Priority:** High | **Type:** Feature

**Story:**
As an **Owner**,
I want to see a household spending summary for the current and previous months,
So that I have an at-a-glance view of where money is going.

**Acceptance Criteria:**
- Given I open the Dashboard, when the page loads within 3 seconds, then I see total spending for the current month, broken down by category.
- Given I select a previous month, when the view updates, then the totals and categories for that month are shown.
- Given I am an Adult Member, when I view the dashboard, then I see only my own expenses and shared expenses I am part of.
- Given I am an Owner, when I view the dashboard, then I can toggle between "Household" and "My Expenses" views.
- Given no expenses exist for a month, when the dashboard loads, then it shows $0 totals with an empty state message.

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

### US-SRCH-03 — Category Trend Report
**Phase:** 2 | **Points:** 5 | **Priority:** Medium | **Type:** Feature

**Story:**
As an **Owner**,
I want to view a spending trend chart per category over the past 6 months,
So that I can identify patterns and make informed budget decisions.

**Acceptance Criteria:**
- Given I navigate to Analytics, when the page loads, then a bar or line chart shows monthly spending per category for the last 6 months.
- Given I select a specific category, when the filter is applied, then the chart focuses on that category's trend only.
- Given spending in a category increased significantly month-over-month (>20%), when displayed, then the spike is visually highlighted.
- Given the chart is shown, when I hover over a data point, then a tooltip shows the exact amount and month.
- Given fewer than 2 months of data exist, when analytics load, then the chart is replaced with a message: "Not enough data yet — add more expenses to see trends."

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

### US-SRCH-04 — Merchant Analytics
**Phase:** 2 | **Points:** 3 | **Priority:** Medium | **Type:** Feature

**Story:**
As an **Owner**,
I want to see how much we spend at each merchant over time,
So that I can identify top vendors and recurring costs.

**Acceptance Criteria:**
- Given I navigate to Merchant Analytics, when the page loads, then a ranked list of merchants appears, sorted by total spend descending.
- Given I click a merchant, when the detail view opens, then I see all expenses at that merchant with dates and amounts.
- Given I filter by date range, when applied, then the merchant rankings update to reflect only spending within that period.
- Given a merchant has only one visit, when displayed, then it still appears in the list (no minimum visit threshold).

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

### US-SRCH-05 — Export Expense Report
**Phase:** 2 | **Points:** 3 | **Priority:** Low | **Type:** Feature

**Story:**
As an **Owner**,
I want to export my expense data as a CSV file for a selected date range,
So that I can use it in spreadsheets or share it with an accountant.

**Acceptance Criteria:**
- Given I select a date range and click "Export CSV," when the export runs, then a CSV file downloads to my device within 3 seconds.
- Given the CSV is downloaded, when I open it, then it includes columns: Date, Merchant, Category, Tags, Amount, Currency, Source (Manual/OCR), Notes.
- Given no expenses exist in the selected range, when export is clicked, then an empty CSV with headers is downloaded (not an error).
- Given I am an Adult Member, when I export, then only my own expenses and shared expenses I am part of are included.

**INVEST:** ✓ Independent ✓ Negotiable ✓ Valuable ✓ Estimable ✓ Small ✓ Testable

---

## Backlog Summary

| Story ID | Title | Epic | Phase | Points | Priority |
|----------|-------|------|-------|--------|----------|
| US-AUTH-01 | User Login | Auth | 1 | 3 | Critical |
| US-AUTH-02 | Multi-Factor Authentication | Auth | 1 | 5 | High |
| US-AUTH-03 | Role-Based Access Control | Auth | 1 | 5 | Critical |
| US-AUTH-04 | User Invitation & Account Setup | Auth | 1 | 3 | High |
| US-AUTH-05 | Audit Logging | Auth | 1 | 5 | High |
| US-REC-01 | Upload Receipt via File/Drag-Drop | Receipt | 1 | 3 | Critical |
| US-REC-02 | Upload Receipt via Mobile Camera | Receipt | 1 | 5 | High |
| US-REC-03 | Attach Multiple Receipts to One Expense | Receipt | 1 | 3 | High |
| US-REC-04 | Image Quality Detection | Receipt | 1 | 3 | Medium |
| US-REC-05 | Automatic Thumbnail Generation | Receipt | 1 | 2 | Medium |
| US-REC-06 | Restricted Member Receipt Upload | Receipt | 1 | 2 | Medium |
| US-OCR-01 | Automatic Receipt Data Extraction | OCR | 1 | 8 | Critical |
| US-OCR-02 | Confidence Scoring Display | OCR | 1 | 3 | High |
| US-OCR-03 | Manual Correction of Extracted Data | OCR | 1 | 5 | Critical |
| US-OCR-04 | Barcode & QR Code Parsing | OCR | 1 | 5 | Medium |
| US-OCR-05 | OCR Retry on Failure | OCR | 1 | 3 | Medium |
| US-EXP-01 | Create Expense Manually | Expense | 1 | 3 | Critical |
| US-EXP-02 | Categorize and Tag an Expense | Expense | 1 | 3 | High |
| US-EXP-03 | Add Notes and Attachments | Expense | 1 | 2 | Medium |
| US-EXP-04 | Mark an Expense as Shared | Expense | 1 | 5 | High |
| US-EXP-05 | View and Edit Expense History | Expense | 1 | 3 | High |
| US-EXP-06 | Item-Level Expense Breakdown | Expense | 1 | 5 | Medium |
| US-BUD-01 | Set Monthly Category Budget | Budget | 2 | 3 | High |
| US-BUD-02 | Household Shared Budget | Budget | 2 | 5 | High |
| US-BUD-03 | Budget Threshold Alerts | Budget | 2 | 5 | Medium |
| US-BUD-04 | Monthly Budget Reset | Budget | 2 | 3 | Medium |
| US-SRCH-01 | Multi-Field Expense Search | Search | 2 | 5 | High |
| US-SRCH-02 | Spending Summary Dashboard | Search | 2 | 5 | High |
| US-SRCH-03 | Category Trend Report | Search | 2 | 5 | Medium |
| US-SRCH-04 | Merchant Analytics | Search | 2 | 3 | Medium |
| US-SRCH-05 | Export Expense Report | Search | 2 | 3 | Low |

**Phase 1 Total:** 84 points across 22 stories
**Phase 2 Total:** 37 points across 9 stories
**Grand Total:** 121 points across 31 stories

---

## Phase 1 Sprint Planning Suggestion

Assuming velocity of ~25 points per 2-week sprint:

| Sprint | Stories | Points | Focus |
|--------|---------|--------|-------|
| Sprint 1 | US-AUTH-01, 03, 04 + US-REC-01, 05 | 16 | Auth foundation + basic upload |
| Sprint 2 | US-AUTH-02, 05 + US-REC-02, 06 | 15 | MFA, audit, mobile upload |
| Sprint 3 | US-OCR-01, 02, 05 | 14 | Core OCR pipeline |
| Sprint 4 | US-OCR-03, 04 + US-REC-03, 04 | 16 | OCR correction + multi-receipt |
| Sprint 5 | US-EXP-01, 02, 03, 05 | 11 | Expense CRUD + categories |
| Sprint 6 | US-EXP-04, 06 | 10 | Shared expenses + item breakdown |
