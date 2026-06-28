BEGIN;

-- Child tables first (FKs cascade, but explicit order is safer)
DELETE FROM expense_attachments;
DELETE FROM expense_shares;
DELETE FROM expense_items;

-- Expenses reference receipts; receipts reference expenses via ExpenseId (no FK).
-- Cascade on receipts → expenses handles expense_items/shares via their own cascades.
DELETE FROM expenses;
DELETE FROM receipts;

DELETE FROM budgets;
DELETE FROM audit_logs;
DELETE FROM invite_tokens;

COMMIT;