'use client'

import { useState } from 'react'
import { useRouter, useParams } from 'next/navigation'
import { apiClient } from '@/api/client'
import type { ActivateResponse } from '@/api/types'
import styles from './activate.module.css'

export default function ActivatePage() {
  const router = useRouter()
  const { token } = useParams<{ token: string }>()
  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  function validate(): string | null {
    if (password.length < 8) return 'Password must be at least 8 characters.'
    if (password !== confirm) return 'Passwords do not match.'
    return null
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    const validationError = validate()
    if (validationError) {
      setError(validationError)
      return
    }
    setError(null)
    setLoading(true)

    try {
      await apiClient.post<ActivateResponse>('/auth/activate', { token, password })
      router.push('/dashboard?welcome=1')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Activation failed.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className={styles.container}>
      <div className={styles.card}>
        <h1 className={styles.title}>Set Your Password</h1>
        <p className={styles.subtitle}>You have been invited to join an Expense Tracker household.</p>

        <form onSubmit={handleSubmit} noValidate className={styles.form}>
          <div className={styles.field}>
            <label htmlFor="password" className={styles.label}>Password</label>
            <input
              id="password"
              type="password"
              autoComplete="new-password"
              required
              minLength={8}
              value={password}
              onChange={e => setPassword(e.target.value)}
              className={styles.input}
            />
          </div>

          <div className={styles.field}>
            <label htmlFor="confirm" className={styles.label}>Confirm Password</label>
            <input
              id="confirm"
              type="password"
              autoComplete="new-password"
              required
              value={confirm}
              onChange={e => setConfirm(e.target.value)}
              className={styles.input}
            />
          </div>

          {error && (
            <p role="alert" className={styles.error}>{error}</p>
          )}

          <button
            type="submit"
            disabled={loading || !password || !confirm}
            className={styles.button}
          >
            {loading ? 'Activating…' : 'Activate Account'}
          </button>
        </form>
      </div>
    </main>
  )
}
