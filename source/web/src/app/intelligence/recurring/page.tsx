'use client'

import { useEffect, useState } from 'react'
import { getRecurring, snoozeRecurring } from '@/api/intelligence'
import type { RecurringExpenseEntry } from '@/api/types'
import styles from './recurring.module.css'

function formatAmount(amount: number): string {
  return `$${amount.toFixed(2)}`
}

function formatDayOfMonth(day: number): string {
  const suffix = day % 10 === 1 && day !== 11 ? 'st'
    : day % 10 === 2 && day !== 12 ? 'nd'
    : day % 10 === 3 && day !== 13 ? 'rd'
    : 'th'
  return `${day}${suffix}`
}

function isSnoozed(entry: RecurringExpenseEntry): boolean {
  return entry.snoozedUntil !== null && new Date(entry.snoozedUntil) > new Date()
}

export default function RecurringExpensesPage() {
  const [items, setItems] = useState<RecurringExpenseEntry[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [snoozing, setSnoozing] = useState<Set<string>>(new Set())
  const [snoozedExpanded, setSnoozedExpanded] = useState(false)

  useEffect(() => {
    void load()
  }, [])

  async function load() {
    setLoading(true)
    setError(null)
    try {
      const res = await getRecurring()
      setItems(res.items)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load recurring expenses.')
    } finally {
      setLoading(false)
    }
  }

  async function handleSnooze(id: string) {
    setSnoozing(prev => new Set(prev).add(id))
    try {
      await snoozeRecurring(id, 30)
      await load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to snooze this recurring expense.')
    } finally {
      setSnoozing(prev => {
        const next = new Set(prev)
        next.delete(id)
        return next
      })
    }
  }

  const active = items.filter(i => !isSnoozed(i))
  const snoozed = items.filter(isSnoozed)

  return (
    <main className={styles.container}>
      <h1 className={styles.pageTitle}>Recurring Expenses</h1>
      <p className={styles.pageSubtitle}>
        Patterns detected automatically from your confirmed expense history.
      </p>

      {error && <p role="alert" className={styles.error}>{error}</p>}

      {loading ? (
        <p className={styles.loadingText}>Loading…</p>
      ) : items.length === 0 ? (
        <div className={styles.emptyState}>
          <p className={styles.emptyText}>
            No recurring patterns detected yet — keep logging expenses and we&apos;ll identify your regular bills.
          </p>
        </div>
      ) : (
        <>
          {active.length === 0 ? (
            <div className={styles.emptyState}>
              <p className={styles.emptyText}>All recurring patterns are currently snoozed.</p>
            </div>
          ) : (
            <ul className={styles.list} role="list">
              {active.map(entry => (
                <li key={entry.id} className={styles.item}>
                  <div className={styles.itemMain}>
                    <span className={styles.merchant}>{entry.merchantNameNormalized}</span>
                    <span
                      className={entry.confidence === 'confirmed' ? styles.badgeConfirmed : styles.badgeLikely}
                    >
                      {entry.confidence === 'confirmed' ? 'Confirmed' : 'Likely'}
                    </span>
                  </div>
                  <div className={styles.itemDetails}>
                    <span>{formatAmount(entry.averageAmount)} / month</span>
                    <span>Around the {formatDayOfMonth(entry.typicalDayOfMonth)}</span>
                  </div>
                  <button
                    type="button"
                    className={styles.snoozeButton}
                    onClick={() => void handleSnooze(entry.id)}
                    disabled={snoozing.has(entry.id)}
                  >
                    {snoozing.has(entry.id) ? 'Snoozing…' : 'Snooze 30 days'}
                  </button>
                </li>
              ))}
            </ul>
          )}

          {snoozed.length > 0 && (
            <div className={styles.snoozedSection}>
              <button
                type="button"
                className={styles.snoozedToggle}
                onClick={() => setSnoozedExpanded(v => !v)}
                aria-expanded={snoozedExpanded}
              >
                {snoozedExpanded ? '▲' : '▼'} Snoozed ({snoozed.length})
              </button>
              {snoozedExpanded && (
                <ul className={styles.list} role="list">
                  {snoozed.map(entry => (
                    <li key={entry.id} className={`${styles.item} ${styles.itemSnoozed}`}>
                      <div className={styles.itemMain}>
                        <span className={styles.merchant}>{entry.merchantNameNormalized}</span>
                        <span
                          className={entry.confidence === 'confirmed' ? styles.badgeConfirmed : styles.badgeLikely}
                        >
                          {entry.confidence === 'confirmed' ? 'Confirmed' : 'Likely'}
                        </span>
                      </div>
                      <div className={styles.itemDetails}>
                        <span>{formatAmount(entry.averageAmount)} / month</span>
                        <span>
                          Snoozed until {entry.snoozedUntil ? new Date(entry.snoozedUntil).toLocaleDateString() : '—'}
                        </span>
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          )}
        </>
      )}
    </main>
  )
}
