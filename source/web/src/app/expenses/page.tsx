'use client'

import { useEffect, useState } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { deleteExpense, getExpenses, getSession } from '@/api/expenses'
import type { ExpenseResponse, SessionResponse } from '@/api/types'
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

  const isAdmin = session?.role === 'Admin'

  async function loadSession() {
    try {
      const s = await getSession()
      setSession(s)
    } catch {
      // Not logged in — middleware should redirect, ignore silently
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
    void loadExpenses()
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [allHousehold, isAdmin])

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
          <Link href="/expenses/new" className={styles.primaryButton}>
            + New Expense
          </Link>
        </div>
      </div>

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
                    {deleteState?.expenseId === expense.id ? (
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
