'use client'

import { useEffect, useRef, useState } from 'react'
import { useRouter } from 'next/navigation'
import QRCode from 'qrcode'
import { apiClient } from '@/api/client'
import type { MfaSetupResponse } from '@/api/types'
import styles from './mfa.module.css'

type SetupStep = 'idle' | 'pending' | 'verifying' | 'success'

export default function MfaSetupPage() {
  const router = useRouter()
  const canvasRef = useRef<HTMLCanvasElement>(null)

  const [step, setStep] = useState<SetupStep>('idle')
  const [secret, setSecret] = useState<string>('')
  const [otpAuthUri, setOtpAuthUri] = useState<string>('')
  const [otp, setOtp] = useState<string>('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [copied, setCopied] = useState(false)

  useEffect(() => {
    if (step === 'pending' && otpAuthUri && canvasRef.current) {
      QRCode.toCanvas(canvasRef.current, otpAuthUri, { width: 200 }, (err) => {
        if (err) {
          setError('Failed to render QR code.')
        }
      })
    }
  }, [step, otpAuthUri])

  async function handleSetup() {
    setError(null)
    setLoading(true)
    try {
      const res = await apiClient.post<MfaSetupResponse>('/auth/mfa/setup', {})
      setSecret(res.secret)
      setOtpAuthUri(res.otpAuthUri)
      setStep('pending')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to start MFA setup.')
    } finally {
      setLoading(false)
    }
  }

  async function handleVerify(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setLoading(true)
    setStep('verifying')

    try {
      await apiClient.post('/auth/mfa/enable', { secret, code: otp })
      setStep('success')
    } catch (err) {
      setStep('pending')
      setError(err instanceof Error ? err.message : 'Invalid code. Please try again.')
    } finally {
      setLoading(false)
    }
  }

  async function handleCopySecret() {
    try {
      await navigator.clipboard.writeText(secret)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch {
      // Clipboard API not available — user can copy manually.
    }
  }

  return (
    <main className={styles.container}>
      <div className={styles.card}>
        <h1 className={styles.title}>Two-Factor Authentication</h1>

        {step === 'idle' && (
          <>
            <p className={styles.description}>
              Protect your account with an authenticator app like Google Authenticator or Authy.
            </p>
            <button
              onClick={handleSetup}
              disabled={loading}
              className={styles.primaryButton}
            >
              {loading ? 'Setting up…' : 'Set Up MFA'}
            </button>
          </>
        )}

        {(step === 'pending' || step === 'verifying') && (
          <>
            <p className={styles.description}>
              Scan this QR code with your authenticator app, then enter the 6-digit code to confirm.
            </p>

            <div className={styles.qrContainer}>
              <canvas ref={canvasRef} aria-label="MFA QR code" />
            </div>

            <div className={styles.secretContainer}>
              <p className={styles.secretLabel}>Or enter this code manually:</p>
              <div className={styles.secretRow}>
                <code className={styles.secretText}>{secret}</code>
                <button
                  type="button"
                  onClick={handleCopySecret}
                  className={styles.copyButton}
                  aria-label="Copy secret to clipboard"
                >
                  {copied ? 'Copied!' : 'Copy'}
                </button>
              </div>
            </div>

            <form onSubmit={handleVerify} noValidate className={styles.form}>
              <div className={styles.field}>
                <label htmlFor="otp" className={styles.label}>
                  Verification Code
                </label>
                <input
                  id="otp"
                  type="text"
                  inputMode="numeric"
                  pattern="[0-9]{6}"
                  maxLength={6}
                  autoComplete="one-time-code"
                  autoFocus
                  required
                  value={otp}
                  onChange={e => setOtp(e.target.value.replace(/\D/g, ''))}
                  className={styles.input}
                  placeholder="000000"
                  aria-describedby={error ? 'mfa-error' : undefined}
                />
              </div>

              {error && (
                <p id="mfa-error" role="alert" className={styles.error}>
                  {error}
                </p>
              )}

              <button
                type="submit"
                disabled={loading || otp.length !== 6}
                className={styles.primaryButton}
              >
                {loading ? 'Verifying…' : 'Verify & Enable MFA'}
              </button>
            </form>
          </>
        )}

        {step === 'success' && (
          <div className={styles.success}>
            <p className={styles.successMessage}>
              MFA is now enabled for your account.
            </p>
            <button
              onClick={() => router.push('/dashboard')}
              className={styles.primaryButton}
            >
              Go to Dashboard
            </button>
          </div>
        )}
      </div>
    </main>
  )
}
