'use client'

import { useCallback, useEffect, useRef, useState } from 'react'
import Link from 'next/link'
import { getReceiptStatus, uploadReceipt } from '@/api/receipts'
import styles from './upload.module.css'

const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000'

const ACCEPTED_MIME = new Set([
  'image/jpeg',
  'image/png',
  'image/heic',
  'image/heif',
  'application/pdf',
])
const MAX_BYTES = 10 * 1024 * 1024

type Phase = 'idle' | 'uploading' | 'processing' | 'complete' | 'failed'

function toAbsoluteUrl(url: string): string {
  return url.startsWith('http') ? url : `${API_BASE}${url}`
}

function validateFile(file: File): string | null {
  const lowerName = file.name.toLowerCase()
  const typeOk = ACCEPTED_MIME.has(file.type) || lowerName.endsWith('.heic') || lowerName.endsWith('.heif')
  if (!typeOk) return 'Accepted formats: JPG, PNG, HEIC, PDF.'
  if (file.size > MAX_BYTES) return 'File too large. Maximum size is 10 MB.'
  return null
}

function IconUploadCloud() {
  return (
    <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" className={styles.dropzoneIcon} aria-hidden="true">
      <polyline points="16 16 12 12 8 16"/>
      <line x1="12" y1="12" x2="12" y2="21"/>
      <path d="M20.39 18.39A5 5 0 0 0 18 9h-1.26A8 8 0 1 0 3 16.3"/>
    </svg>
  )
}

function IconCheck() {
  return (
    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <polyline points="20 6 9 17 4 12"/>
    </svg>
  )
}

function IconX() {
  return (
    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <line x1="18" y1="6" x2="6" y2="18"/>
      <line x1="6" y1="6" x2="18" y2="18"/>
    </svg>
  )
}

export default function UploadReceiptPage() {
  const fileInputRef = useRef<HTMLInputElement>(null)

  const [phase, setPhase] = useState<Phase>('idle')
  const [receiptId, setReceiptId] = useState<string | null>(null)
  const [thumbnailUrl, setThumbnailUrl] = useState<string | null>(null)
  const [retryCount, setRetryCount] = useState(0)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [validationError, setValidationError] = useState<string | null>(null)
  const [dragOver, setDragOver] = useState(false)
  const [pollTick, setPollTick] = useState(0)

  // Poll receipt status every 2 seconds while processing
  useEffect(() => {
    if (phase !== 'processing' || receiptId === null) return
    const id = receiptId

    const timer = setTimeout(async () => {
      try {
        const status = await getReceiptStatus(id)
        if (status.status === 'Complete') {
          if (status.thumbnailUrl) setThumbnailUrl(status.thumbnailUrl)
          setPhase('complete')
        } else if (status.status === 'OcrFailed') {
          setErrorMessage(
            'OCR could not read this receipt. You can enter the expense details manually.'
          )
          setPhase('failed')
        } else {
          setRetryCount(status.ocrRetryCount)
          if (status.thumbnailUrl) setThumbnailUrl(status.thumbnailUrl)
          setPollTick(t => t + 1)
        }
      } catch {
        setPollTick(t => t + 1)
      }
    }, 2000)

    return () => clearTimeout(timer)
  }, [phase, receiptId, pollTick])

  const processFile = useCallback(async (file: File) => {
    const error = validateFile(file)
    if (error) {
      setValidationError(error)
      return
    }

    setValidationError(null)
    setPhase('uploading')

    try {
      const result = await uploadReceipt(file)
      setReceiptId(result.receiptId)
      setThumbnailUrl(result.thumbnailUrl)
      setRetryCount(0)
      setPollTick(0)
      setPhase('processing')
    } catch (err) {
      setErrorMessage(
        err instanceof Error ? err.message : 'Upload failed. Please try again.'
      )
      setPhase('failed')
    }
  }, [])

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.item(0) ?? null
    if (file) void processFile(file)
    // Reset input so the same file can be re-selected after a reset
    e.target.value = ''
  }

  function handleDrop(e: React.DragEvent) {
    e.preventDefault()
    setDragOver(false)
    const file = e.dataTransfer.files.item(0)
    if (file) void processFile(file)
  }

  function handleDragOver(e: React.DragEvent) {
    e.preventDefault()
    setDragOver(true)
  }

  function handleDragLeave(e: React.DragEvent) {
    // Only clear when leaving the dropzone itself, not child elements
    if (!e.currentTarget.contains(e.relatedTarget as Node | null)) {
      setDragOver(false)
    }
  }

  function handleDropzoneKeyDown(e: React.KeyboardEvent) {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault()
      fileInputRef.current?.click()
    }
  }

  function handleReset() {
    setPhase('idle')
    setReceiptId(null)
    setThumbnailUrl(null)
    setRetryCount(0)
    setErrorMessage(null)
    setValidationError(null)
    setDragOver(false)
    setPollTick(0)
  }

  return (
    <main className={styles.container}>
      <div className={styles.header}>
        <h1 className={styles.pageTitle}>Upload Receipt</h1>
        <p className={styles.subtitle}>
          Drag and drop a receipt to extract expense data automatically.
        </p>
      </div>

      {/* Hidden file input — always mounted so ref is stable */}
      <input
        ref={fileInputRef}
        type="file"
        accept=".jpg,.jpeg,.png,.heic,.heif,.pdf,image/jpeg,image/png,image/heic,image/heif,application/pdf"
        style={{ display: 'none' }}
        onChange={handleFileChange}
        aria-hidden="true"
        tabIndex={-1}
      />

      {/* ── Idle ── */}
      {phase === 'idle' && (
        <>
          <div
            className={`${styles.dropzone ?? ''} ${dragOver ? (styles.dropzoneActive ?? '') : ''}`}
            role="button"
            tabIndex={0}
            aria-label="Drop zone — press Enter or Space to select a file"
            onClick={() => fileInputRef.current?.click()}
            onKeyDown={handleDropzoneKeyDown}
            onDragOver={handleDragOver}
            onDragLeave={handleDragLeave}
            onDrop={handleDrop}
          >
            <IconUploadCloud />
            <p className={styles.dropzoneTitle}>Drag and drop your receipt here</p>
            <p className={styles.orDivider}>or</p>
            <button
              type="button"
              className={styles.selectButton}
              onClick={e => {
                e.stopPropagation()
                fileInputRef.current?.click()
              }}
            >
              Select File
            </button>
            <p className={styles.dropzoneMeta}>JPG, PNG, HEIC, PDF · Max 10 MB</p>
          </div>

          {validationError && (
            <p role="alert" className={styles.validationError}>{validationError}</p>
          )}
        </>
      )}

      {/* ── Uploading ── */}
      {phase === 'uploading' && (
        <div className={styles.stateCard}>
          <div className={styles.spinner} role="status" aria-label="Uploading receipt" />
          <p className={styles.stateTitle}>Uploading…</p>
        </div>
      )}

      {/* ── Processing (OCR) ── */}
      {phase === 'processing' && (
        <div className={styles.stateCard}>
          {thumbnailUrl ? (
            <img
              src={toAbsoluteUrl(thumbnailUrl)}
              alt="Receipt thumbnail"
              className={styles.thumbnail}
            />
          ) : (
            <div className={styles.spinner} role="status" aria-label="Processing receipt" />
          )}
          <p className={styles.stateTitle}>OCR is processing your receipt…</p>
          {retryCount > 0 && (
            <span className={styles.retryBadge}>Retry {retryCount} of 3</span>
          )}
          <p className={styles.stateMeta}>This usually takes less than 8 seconds.</p>
        </div>
      )}

      {/* ── Complete ── */}
      {phase === 'complete' && (
        <div className={styles.successCard}>
          <div className={styles.successIconCircle}>
            <IconCheck />
          </div>
          {thumbnailUrl && (
            <img
              src={toAbsoluteUrl(thumbnailUrl)}
              alt="Receipt thumbnail"
              className={styles.thumbnail}
            />
          )}
          <p className={styles.successTitle}>Receipt processed successfully!</p>
          <p className={styles.successMeta}>
            Expense data has been extracted. Open the expense to review and confirm.
          </p>
          <div className={styles.actions}>
            <Link href="/expenses" className={styles.primaryButton}>
              View Expenses
            </Link>
            <button type="button" className={styles.secondaryButton} onClick={handleReset}>
              Upload Another
            </button>
          </div>
        </div>
      )}

      {/* ── Failed ── */}
      {phase === 'failed' && (
        <div className={styles.failedCard}>
          <div className={styles.failedIconCircle}>
            <IconX />
          </div>
          <p className={styles.failedTitle}>OCR Processing Failed</p>
          <p className={styles.failedMeta}>{errorMessage}</p>
          <div className={styles.actions}>
            <Link href="/expenses/new" className={styles.primaryButton}>
              Add Manually
            </Link>
            <button type="button" className={styles.secondaryButton} onClick={handleReset}>
              Try Again
            </button>
          </div>
        </div>
      )}
    </main>
  )
}
