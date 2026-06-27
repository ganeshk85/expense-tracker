import styles from './ReceiptStatusBadge.module.css'

interface ReceiptStatusBadgeProps {
  status: 'Uploaded' | 'Processing' | 'Complete' | 'OcrFailed' | string
  ocrRetryCount?: number
}

export function ReceiptStatusBadge({ status, ocrRetryCount = 0 }: ReceiptStatusBadgeProps) {
  const isProcessing = status === 'Processing'

  const label = (() => {
    if (isProcessing && ocrRetryCount > 0) {
      return `Processing (retry ${ocrRetryCount} of 3)…`
    }
    switch (status) {
      case 'Uploaded':    return 'Uploaded'
      case 'Processing':  return 'Processing…'
      case 'Complete':    return 'Complete'
      case 'OcrFailed':   return 'OCR Failed'
      default:            return status
    }
  })()

  const colorClass = (() => {
    switch (status) {
      case 'Uploaded':    return styles.statusUploaded
      case 'Processing':  return styles.statusProcessing
      case 'Complete':    return styles.statusComplete
      case 'OcrFailed':   return styles.statusFailed
      default:            return styles.statusUploaded
    }
  })()

  return (
    <span className={`${styles.badge} ${colorClass}`}>
      {isProcessing
        ? <span className={styles.spinner} aria-hidden="true" />
        : <span className={styles.dot} aria-hidden="true" />
      }
      {label}
    </span>
  )
}
