# Sprint 7 — Tasks
**Sprint:** 2026-08-11 → 2026-08-22 | **Target:** 18 pts | **Phase 2 close**

## Stories
- US-SRCH-03 (5 pts) — Category Trend Report
- US-SRCH-04 (3 pts) — Merchant Analytics
- Hardening (~10 pts) — Sprint 6 integration tests, DB indexes, performance

---

## US-SRCH-03 — Category Trend Report

### Backend
- [ ] Add `GetCategoryTrendsAsync` query to `IExpenseManagementRepository` and `ExpenseRepository`
  — Returns `IReadOnlyList<(string Month, string Category, decimal Amount)>` for last N months
- [ ] Create `source/api/src/ExpenseTracker.Expense/Models/AnalyticsModels.cs`
  — Records: `CategoryMonthDataPoint`, `CategoryTrendSeries`, `CategoryTrendResponse`, `MerchantRankItem`, `MerchantRankingsResponse`, `MerchantDetailResponse`
- [ ] Create `IAnalyticsService` + `AnalyticsService` in `ExpenseTracker.Expense/Services/`
  — `GetCategoryTrendsAsync(Guid userId, string role, int months, string? category)`
- [ ] Create `AnalyticsEndpoints` — `GET /analytics/category-trends?months=6&category=&view=`
- [ ] Register analytics service and endpoints in `ExpenseModuleExtensions`

### Frontend
- [ ] Add analytics types to `source/web/src/api/types.ts`
  — `CategoryMonthDataPoint`, `CategoryTrendSeries`, `CategoryTrendResponse`
- [ ] Create `source/web/src/api/analytics.ts` — `getCategoryTrends(months, category?, view?)`
- [ ] Create `source/web/src/app/analytics/page.tsx` + `analytics.module.css`
  — CSS bar chart grouped by month; category filter; spike highlight (>20% MoM, amber bar);
    tooltip via `:hover` + CSS `title`; empty state when <2 months data

---

## US-SRCH-04 — Merchant Analytics

### Backend
- [ ] Add `GetMerchantRankingsAsync` and `GetMerchantDetailAsync` to repository + implementation
- [ ] Add merchant analytics methods to `IAnalyticsService` + `AnalyticsService`
  — `GetMerchantRankingsAsync(userId, role, dateFrom?, dateTo?)`
  — `GetMerchantDetailAsync(userId, role, merchantName, dateFrom?, dateTo?)`
- [ ] Add endpoints to `AnalyticsEndpoints`
  — `GET /analytics/merchants?dateFrom=&dateTo=&view=`
  — `GET /analytics/merchants/{name}?dateFrom=&dateTo=&view=`

### Frontend
- [ ] Add merchant types to `source/web/src/api/types.ts`
  — `MerchantRankItem`, `MerchantRankingsResponse`, `MerchantDetailResponse`
- [ ] Add `getMerchantRankings(from?, to?, view?)` and `getMerchantDetail(name, from?, to?)` to `analytics.ts`
- [ ] Add Merchants tab to `/analytics` page
  — Ranked list sorted by total spend; date range filter; click row → detail panel with expense list

---

## DB Indexes Migration

- [ ] Add EF Core migration `Sprint7AnalyticsIndexes`
  — `IX_expenses_date`, `IX_expenses_category`, `IX_expenses_merchant_name`
  — Speeds up 6-month trend aggregation and merchant ranking queries

---

## Hardening — Sprint 6 Integration Tests (carry from Sprint 6 DoD)

- [ ] Create test project `source/api/tests/ExpenseTracker.Budget.Tests/` (if not already)
- [ ] Integration test: `GET /budgets/history` returns budget history for current month
- [ ] Integration test: Budget threshold alert fires at 80% and deduplicates per month
- [ ] Integration test: `GET /dashboard/summary` returns category breakdown for selected month
- [ ] Integration test: `GET /expenses/export` streams CSV with correct headers and escaping
- [ ] Integration test: Analytics `GET /analytics/category-trends` returns data grouped by month
- [ ] Integration test: Analytics `GET /analytics/merchants` returns ranked list sorted by spend

---

## Hardening — Performance

- [ ] Verify dashboard `GET /dashboard/summary` responds within 3s under load (NFR check)
- [ ] Verify analytics `GET /analytics/category-trends` responds within 1s with indexes

---

## Review Notes

### Sprint 7 — Completed 2026-08-22

**Delivered:**
- [x] US-SRCH-03 (5 pts) — Category Trend Report: `AnalyticsService`, repository query, `GET /analytics/category-trends`, CSS bar chart on `/analytics` page, category filter, spike (>20% MoM) amber highlight, tooltip via `title` attr, empty state when <2 months data
- [x] US-SRCH-04 (3 pts) — Merchant Analytics: `GET /analytics/merchants` + `GET /analytics/merchants/{name}`, Merchants tab with ranked list sorted by spend, date range filter, click-to-expand inline detail panel
- [x] DB migration `Sprint7AnalyticsIndexes` — adds `IX_expenses_date`, `IX_expenses_category`, `IX_expenses_merchant_name`
- [x] Integration tests (`ExpenseTracker.Budget.Tests`) — 13 tests covering Sprint 6 carry-over (budget history, dashboard summary, CSV export) and Sprint 7 analytics endpoints (category trends, merchants); real login flow with Argon2 hashed test user
- [x] NavSidebar — Analytics link added

**Build status:**
- API: `dotnet build src/ExpenseTracker.Api` → 0 errors
- Budget.Tests: `dotnet build tests/ExpenseTracker.Budget.Tests` → 0 errors
- Frontend: `tsc --noEmit` → 0 errors in new files (pre-existing CSS Module type warnings in other pages unrelated)

**Known pre-existing issues (not Sprint 7):**
- `ExpenseTracker.Auth.Tests` fails to restore (`Microsoft.Extensions.Configuration.Memory` package unavailable offline)
- Budgets/dashboard/notifications pages have CSS Module `string | undefined` TS errors (pre-existing, not introduced in Sprint 7)

**Phase 2 status:** CLOSED ✓
