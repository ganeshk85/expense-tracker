'use client'

import { useEffect, useState } from 'react'
import { getDashboardSummary } from '@/api/dashboard'
import type { DashboardSummaryResponse } from '@/api/types'
import styles from './dashboard.module.css'

const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
]

function formatMonth(yyyy_mm: string): string {
  const [y, m] = yyyy_mm.split('-')
  return `${MONTH_NAMES[parseInt(m, 10) - 1]} ${y}`
}

function addMonths(yyyy_mm: string, delta: number): string {
  const [y, m] = yyyy_mm.split('-').map(Number)
  const d = new Date(y, m - 1 + delta, 1)
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`
}

function currentMonth(): string {
  const now = new Date()
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`
}

function formatAmount(n: number): string {
  return `$${n.toFixed(2)}`
}

function progressClass(pct: number): string {
  if (pct >= 100) return styles.barRed
  if (pct >= 80) return styles.barAmber
  return styles.barGreen
}

export default function DashboardPage() {
  const [month, setMonth] = useState(currentMonth())
  const [view, setView] = useState<'personal' | 'household'>('personal')
  const [data, setData] = useState<DashboardSummaryResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    void load()
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [month, view])

  async function load() {
    setLoading(true)
    setError(null)
    try {
      const res = await getDashboardSummary(month, view)
      setData(res)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load dashboard.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className={styles.container}>
      <div className={styles.header}>
        <div className={styles.monthNav}>
          <button
            className={styles.navBtn}
            onClick={() => setMonth(m => addMonths(m, -1))}
            aria-label="Previous month"
          >
            ‹
          </button>
          <span className={styles.monthLabel}>{formatMonth(month)}</span>
          <button
            className={styles.navBtn}
            onClick={() => setMonth(m => addMonths(m, 1))}
            aria-label="Next month"
          >
            ›
          </button>
        </div>
        <div className={styles.viewToggle}>
          <button
            className={`${styles.toggleBtn} ${view === 'personal' ? styles.toggleActive : ''}`}
            onClick={() => setView('personal')}
          >
            My View
          </button>
          <button
            className={`${styles.toggleBtn} ${view === 'household' ? styles.toggleActive : ''}`}
            onClick={() => setView('household')}
          >
            Household
          </button>
        </div>
      </div>

      {error && <p role="alert" className={styles.error}>{error}</p>}

      {loading ? (
        <p className={styles.loadingText}>Loading…</p>
      ) : data && data.totalSpent === 0 ? (
        <div className={styles.emptyState}>
          <p className={styles.emptyAmount}>$0.00</p>
          <p className={styles.emptyHint}>No expenses yet — add expenses to see your summary.</p>
        </div>
      ) : data ? (
        <>
          {/* Total spend card */}
          <section className={styles.totalCard} aria-label="Total spending">
            <p className={styles.totalLabel}>Total Spent</p>
            <p className={styles.totalAmount}>{formatAmount(data.totalSpent)}</p>
            <p className={styles.totalSub}>{data.expenseCount} expense{data.expenseCount !== 1 ? 's' : ''}</p>
          </section>

          {/* Category breakdown */}
          {data.categoryBreakdown.length > 0 && (
            <section className={styles.section} aria-label="Category breakdown">
              <h2 className={styles.sectionTitle}>By Category</h2>
              <ul className={styles.breakdownList} role="list">
                {data.categoryBreakdown.map(item => (
                  <li key={item.category} className={styles.breakdownItem}>
                    <div className={styles.breakdownHeader}>
                      <span className={styles.breakdownCategory}>{item.category}</span>
                      <span className={styles.breakdownAmount}>{formatAmount(item.amount)}</span>
                      <span className={styles.breakdownPct}>{item.percentage.toFixed(1)}%</span>
                    </div>
                    <div className={styles.barTrack} role="progressbar" aria-valuenow={item.percentage} aria-valuemin={0} aria-valuemax={100}>
                      <div
                        className={`${styles.barFill} ${progressClass(item.percentage)}`}
                        style={{ width: `${Math.min(item.percentage, 100)}%` }}
                      />
                    </div>
                  </li>
                ))}
              </ul>
            </section>
          )}

          {/* Top merchants */}
          {data.topMerchants.length > 0 && (
            <section className={styles.section} aria-label="Top merchants">
              <h2 className={styles.sectionTitle}>Top Merchants</h2>
              <ol className={styles.merchantList}>
                {data.topMerchants.map((m, i) => (
                  <li key={m.merchant} className={styles.merchantItem}>
                    <span className={styles.merchantRank}>{i + 1}</span>
                    <span className={styles.merchantName}>{m.merchant}</span>
                    <span className={styles.merchantMeta}>{m.visitCount} visit{m.visitCount !== 1 ? 's' : ''}</span>
                    <span className={styles.merchantAmount}>{formatAmount(m.totalSpent)}</span>
                  </li>
                ))}
              </ol>
            </section>
          )}
        </>
      ) : null}
    </main>
  )
}
