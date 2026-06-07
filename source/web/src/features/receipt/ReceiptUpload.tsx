'use client'

import { useRef, useState } from 'react'
import { apiClient } from '@/api/client'
import type { ReceiptStatusResponse, UploadReceiptResponse } from '@/api/types'
import styles from './ReceiptUpload.module.css'

const ACCEPTED_TYPES = ['image/jpeg', 'image/png', 'image/heic', 'image/heif', 'application/pdf']
const ACCEPTED_LABEL = 'JPG, PNG, HEIC, PDF'
const MAX_SIZE_BYTES = 10 * 1024 * 1024

interface ReceiptUploadProps {
  onUploadComplete?: (receiptId: string) => void
}

export function ReceiptUpload({ onUploadComplete }: ReceiptUploadProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [isDragging, setIsDragging] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [progress, setProgress] = useState<'idle' | 'uploading' | 'processing'>('idle')
  const [thumbnailUrl, setThumbnailUrl] = useState<string | null>(null)

  function validateFile(file: File): string | null {
    if (!ACCEPTED_TYPES.includes(file.type)) {
      return `Unsupported file type. Accepted formats: ${ACCEPTED_LABEL}.`
    }
    if (file.size > MAX_SIZE_BYTES) {
      return 'File too large. Maximum size is 10 MB.'
    }
    return null
  }

  async function uploadFile(file: File) {
    const validationError = validateFile(file)
    if (validationError) {
      setError(validationError)
      return
    }

    setError(null)
    setProgress('uploading')

    try {
      const formData = new FormData()
      formData.append('file', file)

      const result = await apiClient.postForm<UploadReceiptResponse>('/receipts/upload', formData)
      setProgress('processing')
      onUploadComplete?.(result.receiptId)
      pollForThumbnail(result.receiptId)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Upload failed. Please try again.')
      setProgress('idle')
    }
  }

  function pollForThumbnail(receiptId: string) {
    const intervalId = setInterval(async () => {
      try {
        const status = await apiClient.get<ReceiptStatusResponse>(`/receipts/${receiptId}/status`)
        if (status.thumbnailUrl) {
          setThumbnailUrl(status.thumbnailUrl)
          setProgress('idle')
          clearInterval(intervalId)
        }
        if (status.status === 'OcrFailed') {
          setProgress('idle')
          clearInterval(intervalId)
        }
      } catch {
        clearInterval(intervalId)
        setProgress('idle')
      }
    }, 1500)

    // Stop polling after 30 seconds regardless
    setTimeout(() => {
      clearInterval(intervalId)
      setProgress('idle')
    }, 30_000)
  }

  function handleDrop(e: React.DragEvent) {
    e.preventDefault()
    setIsDragging(false)
    const file = e.dataTransfer.files[0]
    if (file) uploadFile(file)
  }

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (file) uploadFile(file)
    e.target.value = ''
  }

  const isUploading = progress !== 'idle'

  return (
    <div className={styles.wrapper}>
      <div
        role="region"
        aria-label="Receipt upload area"
        className={[
          styles.dropzone,
          isDragging ? styles.dragging : '',
          isUploading ? styles.uploading : '',
        ].join(' ')}
        onDragOver={e => { e.preventDefault(); setIsDragging(true) }}
        onDragLeave={() => setIsDragging(false)}
        onDrop={handleDrop}
        onClick={() => !isUploading && inputRef.current?.click()}
      >
        <input
          ref={inputRef}
          type="file"
          accept={ACCEPTED_TYPES.join(',')}
          className={styles.hiddenInput}
          onChange={handleFileChange}
          aria-hidden="true"
        />

        {progress === 'uploading' && (
          <div className={styles.status}>
            <span className={styles.spinner} aria-hidden="true" />
            <p>Uploading…</p>
          </div>
        )}

        {progress === 'processing' && !thumbnailUrl && (
          <div className={styles.status}>
            <span className={styles.spinner} aria-hidden="true" />
            <p>Processing receipt…</p>
          </div>
        )}

        {thumbnailUrl && progress === 'idle' && (
          <div className={styles.preview}>
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img src={thumbnailUrl} alt="Receipt preview" className={styles.thumbnail} />
            <p className={styles.previewLabel}>Receipt uploaded</p>
          </div>
        )}

        {progress === 'idle' && !thumbnailUrl && (
          <div className={styles.prompt}>
            <svg className={styles.icon} fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5}
                d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5m-13.5-9L12 3m0 0l4.5 4.5M12 3v13.5" />
            </svg>
            <p className={styles.promptText}>
              <span className={styles.promptLink}>Select a file</span> or drag and drop
            </p>
            <p className={styles.promptHint}>{ACCEPTED_LABEL} up to 10 MB</p>
          </div>
        )}
      </div>

      {error && (
        <p role="alert" className={styles.error}>{error}</p>
      )}
    </div>
  )
}
