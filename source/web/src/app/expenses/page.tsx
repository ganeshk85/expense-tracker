'use client'

import { useEffect, useState } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { deleteExpense, getExpenses, getSession, searchExpenses } from '@/api/expenses'
import type { ExpenseResponse, SearchExpensesParams, SessionResponse } from '@/api/types'
import styles from './expenses.module.css'

type DeleteState = { expenseId: string } | null

// Map known categories to CSS module classes. noUncheckedIndexedAccess requires explicit ?? fallback.
const CATEGORY_CLASSES = {
  Groceries:  styles.badgeGroceries  ?? '',
  Dining:     styles.badgeDining     ?? '',
  Utilities:  styles.badgeUtilities  ?? '',
  Transport:  styles.badgeTransport  ?? '',
  Health:     styles.badgeHealth     ?? '',
} satisfies Record<string, string>

function categoryBadgeClass(cat: string | null): string {
  if (cat !== null && cat in CATEGORY_CLASSES) {
    return CATEGORY_CLASSES[cat as keyof typeof CATEGORY_CLASSES]
  }
  return styles.badgeOther ?? ''
}

function formatDate(iso: string | null): string {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString(undefined, {
    year: 'numeric', month: 'short', day: 'numeric',
  })
}

function formatAmount(amount: number | null): string {
  if (amount === null) return '—'
  return `$${amount.toFixed(2)}`
}

export default function ExpensesPage() {
  const router = useRouter()
  const [session, setSession] = useState<SessionResponse | null>(null)
  const [expenses, setExpenses] = useState<ExpenseResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [allHousehold, setAllHousehold] = useState(false)
  const [deleteState, setDeleteState] = useState<DeleteState>(null)
  const [deleting, setDeleting] = useState(false)
  const [sessionLoaded, setSessionLoaded] = useState(false)

  // Search / filter state (US-SRCH-01)
  const [searchOpen, setSearchOpen] = useState(false)
  const [searchQ, setSearchQ] = useState('')
  const [searchCategory, setSearchCategory] = useState('')
  const [searchMerchant, setSearchMerchant] = useState('')
  const [searchDateFrom, setSearchDateFrom] = useState('')
  const [searchDateTo, setSearchDateTo] = useState('')
  const [searchMinAmount, setSearchMinAmount] = useState('')
  const [searchMaxAmount, setSearchMaxAmount] = useState('')
  const [isSearchActive, setIsSearchActive] = useState(false)

  // Export CSV state
  const [exportOpen, setExportOpen] = useState(false)
  const [exportFrom, setExportFrom] = useState('')
  const [exportTo, setExportTo] = useState('')

  const isAdmin = session?.role === 'Admin'
  const isReader = session?.role === 'Reader'

  async function loadSession() {
    try {
      const s = await getSession()
      setSession(s)
    } catch {
      // Not logged in — middleware should redirect, ignore silently
    } finally {
      setSessionLoaded(true)
    }
  }

  async function loadExpenses() {
    setLoading(true)
    setError(null)
    try {
      const res = await getExpenses({ allHousehold: allHousehold && isAdmin })
      setExpenses(res.items)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load expenses.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void loadSession()
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  useEffect(() => {
    if (!sessionLoaded) return
    void loadExpenses()
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [allHousehold, sessionLoaded])

  async function handleDelete(id: string) {
    setDeleting(true)
    try {
      await deleteExpense(id)
      setExpenses(prev => prev.filter(e => e.id !== id))
      setDeleteState(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete expense.')
    } finally {
      setDeleting(false)
    }
  }

  async function handleSearch(e: React.FormEvent) {
    e.preventDefault()
    setLoading(true)
    setError(null)
    const params: SearchExpensesParams = {}
    if (searchQ) params.q = searchQ
    if (searchCategory) params.category = searchCategory
    if (searchMerchant) params.merchant = searchMerchant
    if (searchDateFrom) params.dateFrom = new Date(searchDateFrom).toISOString()
    if (searchDateTo) params.dateTo = new Date(searchDateTo).toISOString()
    if (searchMinAmount) params.minAmount = parseFloat(searchMinAmount)
    if (searchMaxAmount) params.maxAmount = parseFloat(searchMaxAmount)
    try {
      const res = await searchExpenses(params)
      setExpenses(res.items)
      setIsSearchActive(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Search failed.')
    } finally {
      setLoading(false)
    }
  }

  function handleClearSearch() {
    setSearchQ('')
    setSearchCategory('')
    setSearchMerchant('')
    setSearchDateFrom('')
    setSearchDateTo('')
    setSearchMinAmount('')
    setSearchMaxAmount('')
    setIsSearchActive(false)
    void loadExpenses()
  }

  function handleRowClick(e: React.MouseEvent, id: string) {
    // Prevent row navigation when clicking the delete button area
    const target = e.target as HTMLElement
    if (target.closest('button')) return
    router.push(`/expenses/${id}`)
  }

  return (
    <main className={styles.container}>
      <div className={styles.header}>
        <h1 className={styles.pageTitle}>Expenses</h1>
        <div className={styles.headerActions}>
          {isAdmin && (
            <div className={styles.toggleGroup} role="group" aria-label="View scope">
              <button
                className={`${styles.toggleBtn} ${!allHousehold ? styles.toggleBtnActive : ''}`}
                onClick={() => setAllHousehold(false)}
              >
                My Expenses
              </button>
              <button
                className={`${styles.toggleBtn} ${allHousehold ? styles.toggleBtnActive : ''}`}
                onClick={() => setAllHousehold(true)}
              >
                All Household
              </button>
            </div>
          )}
          <button
            className={styles.secondaryButton}
            onClick={() => setSearchOpen(v => !v)}
            aria-expanded={searchOpen}
          >
            {searchOpen ? 'Hide Filters' : 'Search / Filter'}
          </button>
          <button
            className={styles.secondaryButton}
            onClick={() => setExportOpen(v => !v)}
            aria-expanded={exportOpen}
          >
            Export CSV
          </button>
          {!isReader && (
            <Link href="/expenses/new" className={styles.primaryButton}>
              + New Expense
            </Link>
          )}
        </div>
      </div>

      {/* Export CSV inline form */}
      {exportOpen && (
        <div className={styles.exportPanel}>
          <p className={styles.exportTitle}>Download Expenses as CSV</p>
          <div className={styles.exportFields}>
            <div className={styles.exportField}>
              <label htmlFor="exportFrom" className={styles.exportLabel}>From</label>
              <input
                id="exportFrom"
                type="date"
                className={styles.exportInput}
                value={exportFrom}
                onChange={e => setExportFrom(e.target.value)}
              />
            </div>
            <div className={styles.exportField}>
              <label htmlFor="exportTo" className={styles.exportLabel}>To</label>
              <input
                id="exportTo"
                type="date"
                className={styles.exportInput}
                value={exportTo}
                onChange={e => setExportTo(e.target.value)}
              />
            </div>
            <a
              href={`/api/expenses/export${exportFrom || exportTo ? `?from=${exportFrom}&to=${exportTo}` : ''}`}
              className={styles.primaryButton}
              download
              onClick={() => setExportOpen(false)}
            >
              Download
            </a>
          </div>
        </div>
      )}

      {/* US-REC-06: Reader banner */}
      {isReader && (
        <div role="status" className={styles.readerBanner}>
          You have view-only access. You can view expenses but cannot create or delete them.
        </div>
      )}

      {/* US-SRCH-01: Search panel */}
      {searchOpen && (
        <form onSubmit={handleSearch} className={styles.searchPanel} noValidate>
          <div className={styles.searchGrid}>
            <div className={styles.searchField}>
              <label htmlFor="searchQ" className={styles.searchLabel}>Keywords</label>
              <input
                id="searchQ"
                type="text"
                className={styles.searchInput}
                value={searchQ}
                onChange={e => setSearchQ(e.target.value)}
                placeholder="Merchant name or notes…"
              />
            </div>
            <div className={styles.searchField}>
              <label htmlFor="searchCategory" className={styles.searchLabel}>Category</label>
              <select
                id="searchCategory"
                className={styles.searchInput}
                value={searchCategory}
                onChange={e => setSearchCategory(e.target.value)}
              >
                <option value="">All categories</option>
                {['Groceries', 'Dining', 'Utilities', 'Transport', 'Health', 'Other'].map(c => (
                  <option key={c} value={c}>{c}</option>
                ))}
              </select>
            </div>
            <div className={styles.searchField}>
              <label htmlFor="searchMerchant" className={styles.searchLabel}>Merchant</label>
              <input
                id="searchMerchant"
                type="text"
                className={styles.searchInput}
                value={searchMerchant}
                onChange={e => setSearchMerchant(e.target.value)}
                placeholder="Merchant name…"
              />
            </div>
            <div className={styles.searchField}>
              <label htmlFor="searchDateFrom" className={styles.searchLabel}>Date from</label>
              <input
                id="searchDateFrom"
                type="date"
                className={styles.searchInput}
                value={searchDateFrom}
                onChange={e => setSearchDateFrom(e.target.value)}
              />
            </div>
            <div className={styles.searchField}>
              <label htmlFor="searchDateTo" className={styles.searchLabel}>Date to</label>
              <input
                id="searchDateTo"
                type="date"
                className={styles.searchInput}
                value={searchDateTo}
                onChange={e => setSearchDateTo(e.target.value)}
              />
            </div>
            <div className={styles.searchField}>
              <label htmlFor="searchMinAmount" className={styles.searchLabel}>Min amount</label>
              <input
                id="searchMinAmount"
                type="number"
                className={styles.searchInput}
                value={searchMinAmount}
                onChange={e => setSearchMinAmount(e.target.value)}
                placeholder="0.00"
                min="0"
                step="0.01"
              />
            </div>
            <div className={styles.searchField}>
              <label htmlFor="searchMaxAmount" className={styles.searchLabel}>Max amount</label>
              <input
                id="searchMaxAmount"
                type="number"
                className={styles.searchInput}
                value={searchMaxAmount}
                onChange={e => setSearchMaxAmount(e.target.value)}
                placeholder="0.00"
                min="0"
                step="0.01"
              />
            </div>
          </div>
          <div className={styles.searchActions}>
            <button type="submit" className={styles.primaryButton}>Search</button>
            {isSearchActive && (
              <button type="button" className={styles.secondaryButton} onClick={handleClearSearch}>
                Clear
              </button>
            )}
          </div>
          {isSearchActive && (
            <p className={styles.searchActiveNote}>Showing search results. Clear filters to restore full list.</p>
          )}
        </form>
      )}

      {error && <p role="alert" className={styles.error}>{error}</p>}

      {loading ? (
        <p className={styles.loadingText}>Loading expenses…</p>
      ) : (
        <div className={styles.tableWrapper}>
          <table className={styles.table}>
            <thead>
              <tr>
                <th className={styles.th}>Merchant</th>
                <th className={styles.th}>Date</th>
                <th className={styles.th}>Total</th>
                <th className={styles.th}>Category</th>
                <th className={styles.th}>Source</th>
                <th className={styles.th}>Tags</th>
                <th className={styles.th}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {expenses.map(expense => (
                <tr
                  key={expense.id}
                  className={styles.tr}
                  onClick={e => handleRowClick(e, expense.id)}
                >
                  <td className={styles.td}>
                    <span className={styles.merchantCell}>
                      {expense.merchantName ?? <span className={styles.tdMuted}>—</span>}
                      {expense.isShared && (
                        <span className={`${styles.badge} ${styles.sharedBadge}`}>Shared</span>
                      )}
                    </span>
                  </td>
                  <td className={styles.td}>{formatDate(expense.date)}</td>
                  <td className={`${styles.td} ${styles.total}`}>
                    {formatAmount(expense.total)}
                  </td>
                  <td className={styles.td}>
                    {expense.category ? (
                      <span className={`${styles.badge} ${categoryBadgeClass(expense.category)}`}>
                        {expense.category}
                      </span>
                    ) : (
                      <span className={styles.tdMuted}>—</span>
                    )}
                  </td>
                  <td className={styles.td}>
                    <span className={`${styles.badge} ${expense.source === 'OCR' ? styles.sourceOCR : styles.sourceManual}`}>
                      {expense.source}
                    </span>
                  </td>
                  <td className={styles.td}>
                    {expense.tags.length > 0 ? (
                      <div className={styles.tagList}>
                        {expense.tags.map(tag => (
                          <span key={tag} className={styles.tag}>{tag}</span>
                        ))}
                      </div>
                    ) : (
                      <span className={styles.tdMuted}>—</span>
                    )}
                  </td>
                  <td className={styles.tdActions}>
                    {!isReader && (
                      deleteState?.expenseId === expense.id ? (
                        <span className={styles.confirmInline}>
                          Delete?
                          <button
                            className={styles.confirmYes}
                            disabled={deleting}
                            onClick={() => handleDelete(expense.id)}
                          >
                            Yes
                          </button>
                          <button
                            className={styles.confirmNo}
                            disabled={deleting}
                            onClick={() => setDeleteState(null)}
                          >
                            No
                          </button>
                        </span>
                      ) : (
                        <button
                          className={styles.deleteButton}
                          onClick={() => setDeleteState({ expenseId: expense.id })}
                        >
                          Delete
                        </button>
                      )
                    )}
                  </td>
                </tr>
              ))}

              {expenses.length === 0 && (
                <tr>
                  <td colSpan={7} className={styles.emptyCell}>
                    No expenses yet. <Link href="/expenses/new">Add one manually</Link> or upload a receipt.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </main>
  )
}
