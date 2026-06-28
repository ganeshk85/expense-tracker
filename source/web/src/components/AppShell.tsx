'use client'

import { usePathname } from 'next/navigation'
import { NavSidebar } from './NavSidebar'
import styles from './AppShell.module.css'

const PUBLIC_PREFIXES = ['/login', '/invite']

function isPublicRoute(pathname: string): boolean {
  return PUBLIC_PREFIXES.some(
    prefix => pathname === prefix || pathname.startsWith(prefix + '/')
  )
}

interface AppShellProps {
  children: React.ReactNode
}

export function AppShell({ children }: AppShellProps) {
  const pathname = usePathname()

  if (isPublicRoute(pathname)) {
    return <>{children}</>
  }

  return (
    <div className={styles.shell}>
      <NavSidebar />
      <div className={styles.content}>{children}</div>
    </div>
  )
}
