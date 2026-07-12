'use client'

import Link from 'next/link'
import { useEffect, useState } from 'react'
import { usePathname } from 'next/navigation'
import { getSession } from '@/api/expenses'
import { getNotifications } from '@/api/notifications'
import styles from './NavSidebar.module.css'

function IconDashboard() {
  return (
    <svg className={styles.navIcon} width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <rect x="3" y="3" width="7" height="7" rx="1"/>
      <rect x="14" y="3" width="7" height="7" rx="1"/>
      <rect x="14" y="14" width="7" height="7" rx="1"/>
      <rect x="3" y="14" width="7" height="7" rx="1"/>
    </svg>
  )
}

function IconUpload() {
  return (
    <svg className={styles.navIcon} width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
      <polyline points="17 8 12 3 7 8"/>
      <line x1="12" y1="3" x2="12" y2="15"/>
    </svg>
  )
}

function IconExpenses() {
  return (
    <svg className={styles.navIcon} width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <line x1="8" y1="6" x2="21" y2="6"/>
      <line x1="8" y1="12" x2="21" y2="12"/>
      <line x1="8" y1="18" x2="21" y2="18"/>
      <line x1="3" y1="6" x2="3.01" y2="6"/>
      <line x1="3" y1="12" x2="3.01" y2="12"/>
      <line x1="3" y1="18" x2="3.01" y2="18"/>
    </svg>
  )
}

function IconBudget() {
  return (
    <svg className={styles.navIcon} width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <circle cx="12" cy="12" r="10"/>
      <path d="M12 6v6l4 2"/>
    </svg>
  )
}

function IconBell() {
  return (
    <svg className={styles.navIcon} width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/>
      <path d="M13.73 21a2 2 0 0 1-3.46 0"/>
    </svg>
  )
}

function IconAnalytics() {
  return (
    <svg className={styles.navIcon} width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <line x1="18" y1="20" x2="18" y2="10"/>
      <line x1="12" y1="20" x2="12" y2="4"/>
      <line x1="6" y1="20" x2="6" y2="14"/>
    </svg>
  )
}

function IconRepeat() {
  return (
    <svg className={styles.navIcon} width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <polyline points="17 1 21 5 17 9"/>
      <path d="M3 11V9a4 4 0 0 1 4-4h14"/>
      <polyline points="7 23 3 19 7 15"/>
      <path d="M21 13v2a4 4 0 0 1-4 4H3"/>
    </svg>
  )
}

function IconSettings() {
  return (
    <svg className={styles.navIcon} width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <circle cx="12" cy="12" r="3"/>
      <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"/>
    </svg>
  )
}

function isActive(prefix: string, pathname: string): boolean {
  return pathname === prefix || pathname.startsWith(prefix + '/')
}

export function NavSidebar() {
  const pathname = usePathname()
  const [unreadCount, setUnreadCount] = useState(0)
  const [isAdmin, setIsAdmin] = useState(false)

  async function fetchUnreadCount() {
    try {
      const res = await getNotifications()
      setUnreadCount(res.notifications.length)
    } catch {
      // Silently ignore — sidebar badge is non-critical
    }
  }

  useEffect(() => {
    void fetchUnreadCount()
    const interval = setInterval(() => { void fetchUnreadCount() }, 30_000)
    return () => clearInterval(interval)
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  useEffect(() => {
    async function loadRole() {
      try {
        const session = await getSession()
        setIsAdmin(session.role === 'Admin')
      } catch {
        // Not logged in — middleware handles redirect elsewhere
      }
    }
    void loadRole()
  }, [])

  function linkClass(prefix: string): string {
    return `${styles.navLink ?? ''} ${isActive(prefix, pathname) ? (styles.navLinkActive ?? '') : ''}`
  }

  function ariaCurrent(prefix: string): 'page' | undefined {
    return isActive(prefix, pathname) ? 'page' : undefined
  }

  return (
    <nav className={styles.sidebar} aria-label="Main navigation">
      <div className={styles.brand}>
        <div>
          <span className={styles.brandName}>Expense Tracker</span>
          <span className={styles.brandSub}>Family Finance</span>
        </div>
      </div>

      <ul className={styles.navList} role="list">
        <li>
          <Link href="/dashboard" className={linkClass('/dashboard')} aria-current={ariaCurrent('/dashboard')}>
            <IconDashboard />
            Dashboard
          </Link>
        </li>
        <li>
          <Link href="/receipts/upload" className={linkClass('/receipts')} aria-current={ariaCurrent('/receipts')}>
            <IconUpload />
            Upload Receipt
          </Link>
        </li>
        <li>
          <Link href="/expenses" className={linkClass('/expenses')} aria-current={ariaCurrent('/expenses')}>
            <IconExpenses />
            Expenses
          </Link>
        </li>
        <li>
          <Link href="/budgets" className={linkClass('/budgets')} aria-current={ariaCurrent('/budgets')}>
            <IconBudget />
            Budgets
          </Link>
        </li>
        <li>
          <Link href="/analytics" className={linkClass('/analytics')} aria-current={ariaCurrent('/analytics')}>
            <IconAnalytics />
            Analytics
          </Link>
        </li>
        <li>
          <Link href="/notifications" className={linkClass('/notifications')} aria-current={ariaCurrent('/notifications')}>
            <span className={styles.bellWrapper}>
              <IconBell />
              {unreadCount > 0 && (
                <span className={styles.badge} aria-label={`${unreadCount} unread notifications`}>
                  {unreadCount > 9 ? '9+' : unreadCount}
                </span>
              )}
            </span>
            Notifications
          </Link>
        </li>
        <li>
          <Link href="/intelligence/recurring" className={linkClass('/intelligence')} aria-current={ariaCurrent('/intelligence')}>
            <IconRepeat />
            Recurring
          </Link>
        </li>
        <li>
          <Link href="/settings/members" className={linkClass('/settings/members')} aria-current={ariaCurrent('/settings/members')}>
            <IconSettings />
            Settings
          </Link>
        </li>
        {isAdmin && (
          <li>
            <Link
              href="/settings/intelligence"
              className={linkClass('/settings/intelligence')}
              aria-current={ariaCurrent('/settings/intelligence')}
            >
              <IconSettings />
              Intelligence Settings
            </Link>
          </li>
        )}
      </ul>
    </nav>
  )
}
