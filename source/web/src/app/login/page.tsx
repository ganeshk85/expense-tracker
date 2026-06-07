'use client'

import { useState } from 'react'
import { useRouter } from 'next/navigation'
import { apiClient } from '@/api/client'
import type { LoginResponse } from '@/api/types'
import styles from './login.module.css'

export default function LoginPage() {
  const router = useRouter()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setLoading(true)

    try {
      const res = await apiClient.post<LoginResponse & { mfaRequired?: boolean }>(
        '/auth/login',
        { username, password }
      )

      if (res.mfaRequired) {
        router.push('/login/mfa')
      } else {
        router.push('/dashboard')
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className={styles.container}>
      <div className={styles.card}>
        <h1 className={styles.title}>Expense Tracker</h1>
        <p className={styles.subtitle}>Sign in to your household account</p>

        <form onSubmit={handleSubmit} noValidate className={styles.form}>
          <div className={styles.field}>
            <label htmlFor="username" className={styles.label}>Username</label>
            <input
              id="username"
              type="text"
              autoComplete="username"
              required
              value={username}
              onChange={e => setUsername(e.target.value)}
              className={styles.input}
              aria-describedby={error ? 'login-error' : undefined}
            />
          </div>

          <div className={styles.field}>
            <label htmlFor="password" className={styles.label}>Password</label>
            <input
              id="password"
              type="password"
              autoComplete="current-password"
              required
              value={password}
              onChange={e => setPassword(e.target.value)}
              className={styles.input}
            />
          </div>

          {error && (
            <p id="login-error" role="alert" className={styles.error}>
              {error}
            </p>
          )}

          <button
            type="submit"
            disabled={loading || !username || !password}
            className={styles.button}
          >
            {loading ? 'Signing in…' : 'Sign in'}
          </button>
        </form>
      </div>
    </main>
  )
}
