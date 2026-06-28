'use client'

import { useEffect, useState } from 'react'
import { dismissNotification, getNotifications } from '@/api/notifications'
import type { NotificationResponse } from '@/api/types'
import styles from './notifications.module.css'

const TYPE_LABELS: Record<string, string> = {
  budget_threshold: 'Approaching Limit',
  budget_exceeded: 'Limit Exceeded',
  budget_deleted: 'Budget Removed',
}

const TYPE_BADGE_CLASS: Record<string, string> = {
  budget_threshold: styles.badgeAmber,
  budget_exceeded: styles.badgeRed,
  budget_deleted: styles.badgeGrey,
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

export default function NotificationsPage() {
  const [notifications, setNotifications] = useState<NotificationResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [dismissing, setDismissing] = useState<Set<string>>(new Set())

  useEffect(() => {
    void load()
  }, [])

  async function load() {
    try {
      const res = await getNotifications()
      setNotifications(res.notifications)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load notifications.')
    } finally {
      setLoading(false)
    }
  }

  async function handleDismiss(id: string) {
    setDismissing(prev => new Set(prev).add(id))
    // Optimistic update
    setNotifications(prev => prev.filter(n => n.id !== id))
    try {
      await dismissNotification(id)
    } catch {
      // On failure, reload to restore accurate state
      const res = await getNotifications()
      setNotifications(res.notifications)
    } finally {
      setDismissing(prev => {
        const next = new Set(prev)
        next.delete(id)
        return next
      })
    }
  }

  return (
    <main className={styles.container}>
      <h1 className={styles.pageTitle}>Notifications</h1>

      {error && <p role="alert" className={styles.error}>{error}</p>}

      {loading ? (
        <p className={styles.loadingText}>Loading…</p>
      ) : notifications.length === 0 ? (
        <div className={styles.emptyState}>
          <p className={styles.emptyIcon}>✓</p>
          <p className={styles.emptyText}>No alerts — you&apos;re on track!</p>
        </div>
      ) : (
        <ul className={styles.list} role="list">
          {notifications.map(n => (
            <li key={n.id} className={styles.item}>
              <div className={styles.itemHeader}>
                <span className={`${styles.badge} ${TYPE_BADGE_CLASS[n.type] ?? styles.badgeGrey}`}>
                  {TYPE_LABELS[n.type] ?? n.type}
                </span>
                <span className={styles.date}>{formatDate(n.createdAt)}</span>
              </div>
              <p className={styles.message}>{n.message}</p>
              <div className={styles.itemFooter}>
                <button
                  className={styles.dismissBtn}
                  onClick={() => void handleDismiss(n.id)}
                  disabled={dismissing.has(n.id)}
                  aria-label="Dismiss notification"
                >
                  {dismissing.has(n.id) ? 'Dismissing…' : 'Dismiss'}
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </main>
  )
}
