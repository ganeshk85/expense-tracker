import type { Metadata } from 'next'
import './globals.css'

export const metadata: Metadata = {
  title: 'Expense Tracker',
  description: 'Family Expense Intelligence Platform',
}

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  )
}
