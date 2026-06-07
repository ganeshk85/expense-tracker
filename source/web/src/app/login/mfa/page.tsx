'use client'

import { useState } from 'react'
import { useRouter } from 'next/navigation'
import { apiClient } from '@/api/client'
import styles from '../login.module.css'

export default function MfaPage() {
  const router = useRouter()
  const [code, setCode] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setLoading(true)

    try {
      await apiClient.post('/auth/mfa/login', { code })
      router.push('/dashboard')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Invalid code. Please try again.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className={styles.container}>
      <div className={styles.card}>
        <h1 className={styles.title}>Two-Factor Verification</h1>
        <p className={styles.subtitle}>Enter the 6-digit code from your authenticator app</p>

        <form onSubmit={handleSubmit} noValidate className={styles.form}>
          <div className={styles.field}>
            <label htmlFor="code" className={styles.label}>Authentication Code</label>
            <input
              id="code"
              type="text"
              inputMode="numeric"
              pattern="[0-9]{6}"
              maxLength={6}
              autoComplete="one-time-code"
              required
              value={code}
              onChange={e => setCode(e.target.value.replace(/\D/g, ''))}
              className={styles.input}
              placeholder="000000"
            />
          </div>

          {error && (
            <p role="alert" className={styles.error}>{error}</p>
          )}

          <button
            type="submit"
            disabled={loading || code.length !== 6}
            className={styles.button}
          >
            {loading ? 'Verifying…' : 'Verify'}
          </button>
        </form>
      </div>
    </main>
  )
}
