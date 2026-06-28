'use client'

import { use, useEffect, useRef, useState, KeyboardEvent } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import {
  assignShares,
  attachReceipt,
  correctExpense,
  deleteAttachment,
  deleteExpense,
  detachReceipt,
  getAttachments,
  getExpense,
  getSession,
  updateExpense,
  uploadAttachment,
} from '@/api/expenses'
import type {
  ExpenseAttachmentResponse,
  ExpenseResponse,
  ExpenseShareResponse,
  ReceiptSummaryResponse,
  SessionResponse,
} from '@/api/types'
import styles from './expense-detail.module.css'

const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000'
const CATEGORIES = ['Groceries', 'Dining', 'Utilities', 'Transport', 'Health', 'Other']

function toAbsoluteUrl(url: string): string {
  return url.startsWith('http') ? url : `${API_BASE}${url}`
}

// ── Sub-components ─────────────────────────────────────────────────────────────

interface ItemForm {
  id?: string
  name: string
  quantity: string
  unitPrice: string
}

interface Confidence {
  merchantName?: number
  date?: number
  total?: number
}

function parseConfidence(json: string | null): Confidence {
  if (!json) return {}
  try {
    return JSON.parse(json) as Confidence
  } catch {
    return {}
  }
}

function ConfidenceIndicator({ score }: { score?: number }) {
  if (score === undefined) return null
  if (score >= 90) {
    return (
      <span className={styles.confidenceHigh} title={`${score}% confidence`}>
        <span className={styles.confidenceDot} />
        {score}%
      </span>
    )
  }
  if (score >= 70) {
    return (
      <span className={styles.confidenceMedium} title={`${score}% confidence — review recommended`}>
        <span className={styles.confidenceDot} />
        Review ({score}%)
      </span>
    )
  }
  return (
    <span className={styles.confidenceLow} title={`${score}% confidence — low accuracy`}>
      <span className={styles.confidenceDot} />
      Low ({score}%)
    </span>
  )
}

function itemsTotal(items: ItemForm[]): number {
  return items.reduce((sum, i) => {
    const qty = parseFloat(i.quantity) || 0
    const price = parseFloat(i.unitPrice) || 0
    return sum + qty * price
  }, 0)
}

// ── Shared expense row editor ─────────────────────────────────────────────────

interface ShareRow {
  userId: string
  username: string
  value: string // amount or percentage string
}

// ── Main page ─────────────────────────────────────────────────────────────────

export default function ExpenseDetailPage({
  params,
}: {
  params: Promise<{ id: string }>
}) {
  const { id } = use(params)
  const router = useRouter()

  const [expense, setExpense] = useState<ExpenseResponse | null>(null)
  const [session, setSession] = useState<SessionResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [showDeleteDialog, setShowDeleteDialog] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [confirming, setConfirming] = useState(false)

  // Section collapse state
  const [itemsExpanded, setItemsExpanded] = useState(true)
  const [sharesExpanded, setSharesExpanded] = useState(true)
  const [receiptsExpanded, setReceiptsExpanded] = useState(true)

  // Form state
  const [merchantName, setMerchantName] = useState('')
  const [merchantAddress, setMerchantAddress] = useState('')
  const [date, setDate] = useState('')
  const [time, setTime] = useState('')
  const [total, setTotal] = useState('')
  const [subtotal, setSubtotal] = useState('')
  const [taxAmount, setTaxAmount] = useState('')
  const [category, setCategory] = useState('')
  const [tags, setTags] = useState<string[]>([])
  const [pendingTag, setPendingTag] = useState('')
  const [notes, setNotes] = useState('')
  const [items, setItems] = useState<ItemForm[]>([])

  // US-EXP-04: Shared expense state
  const [isShared, setIsShared] = useState(false)
  const [splitType, setSplitType] = useState<'amount' | 'percentage'>('percentage')
  const [shareRows, setShareRows] = useState<ShareRow[]>([])
  const [assigningShares, setAssigningShares] = useState(false)
  const [sharesOutOfSync, setSharesOutOfSync] = useState(false)

  // US-REC-03: Receipt gallery state
  const [attachReceiptId, setAttachReceiptId] = useState('')
  const [attaching, setAttaching] = useState(false)
  const [detachTarget, setDetachTarget] = useState<string | null>(null)

  // US-EXP-03: File attachments state
  const [attachments, setAttachments] = useState<ExpenseAttachmentResponse[]>([])
  const [attachmentsExpanded, setAttachmentsExpanded] = useState(true)
  const [uploadingFile, setUploadingFile] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const today = new Date().toISOString().split('T').at(0) ?? ''

  function populateForm(e: ExpenseResponse) {
    setMerchantName(e.merchantName ?? '')
    setMerchantAddress(e.merchantAddress ?? '')
    setDate(e.date ? (e.date.split('T').at(0) ?? '') : '')
    setTime(e.time ?? '')
    setTotal(e.total !== null ? String(e.total) : '')
    setSubtotal(e.subtotal !== null ? String(e.subtotal) : '')
    setTaxAmount(e.taxAmount !== null ? String(e.taxAmount) : '')
    setCategory(e.category ?? '')
    setTags(e.tags)
    setNotes(e.notes ?? '')
    setItems(e.items.map(i => ({
      id: i.id,
      name: i.name,
      quantity: String(i.quantity),
      unitPrice: String(i.unitPrice),
    })))
    setIsShared(e.isShared)
    // Build share rows from existing shares (no username lookup — showing userId for now)
    setShareRows(
      e.shares.map(s => ({
        userId: s.userId,
        username: s.userId, // will be resolved from members list in future
        value: splitType === 'percentage'
          ? String(s.percentage ?? '')
          : String(s.amount ?? ''),
      }))
    )
    setSharesOutOfSync(false)
  }

  useEffect(() => {
    async function load() {
      try {
        const [e, sess, attachList] = await Promise.all([
          getExpense(id),
          getSession(),
          getAttachments(id),
        ])
        setExpense(e)
        setSession(sess)
        setAttachments(attachList.attachments)
        populateForm(e)
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load expense.')
      } finally {
        setLoading(false)
      }
    }
    void load()
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id])

  const confidence = parseConfidence(expense?.confidenceJson ?? null)
  const hasOcrData =
    expense?.source === 'OCR' &&
    expense.ocrStatus === 'complete' &&
    expense.confidenceJson !== null

  const computedItemsTotal = itemsTotal(items)
  const parsedTotal = parseFloat(total) || 0
  const showTotalMismatch =
    items.length > 0 &&
    parsedTotal > 0 &&
    Math.abs(computedItemsTotal - parsedTotal) > 0.01

  // ── Tag helpers ──────────────────────────────────────────────────────────────

  function addTag() {
    const trimmed = pendingTag.trim()
    if (trimmed && !tags.includes(trimmed)) setTags(prev => [...prev, trimmed])
    setPendingTag('')
  }
  function removeTag(tag: string) { setTags(prev => prev.filter(t => t !== tag)) }
  function handleTagKeyDown(e: KeyboardEvent<HTMLInputElement>) {
    if (e.key === 'Enter') { e.preventDefault(); addTag() }
    else if (e.key === 'Backspace' && pendingTag === '' && tags.length > 0) {
      setTags(prev => prev.slice(0, -1))
    }
  }

  // ── Item helpers ─────────────────────────────────────────────────────────────

  function updateItem(idx: number, field: keyof ItemForm, value: string) {
    setItems(prev => prev.map((item, i) => i === idx ? { ...item, [field]: value } : item))
  }
  function removeItem(idx: number) { setItems(prev => prev.filter((_, i) => i !== idx)) }
  function addItem() {
    setItems(prev => [...prev, { name: '', quantity: '1', unitPrice: '0' }])
  }
  function buildItemsPayload() {
    return items.map(i => ({
      id: i.id,
      name: i.name,
      quantity: parseFloat(i.quantity) || 0,
      unitPrice: parseFloat(i.unitPrice) || 0,
    }))
  }

  // ── Share helpers ────────────────────────────────────────────────────────────

  function updateShareRow(idx: number, value: string) {
    setShareRows(prev => prev.map((r, i) => i === idx ? { ...r, value } : r))
  }

  function addShareRow() {
    setShareRows(prev => [...prev, { userId: '', username: '', value: '' }])
  }

  function removeShareRow(idx: number) {
    setShareRows(prev => prev.filter((_, i) => i !== idx))
  }

  function sharesSum(): number {
    return shareRows.reduce((sum, r) => sum + (parseFloat(r.value) || 0), 0)
  }

  function sharesValid(): boolean {
    if (shareRows.length === 0) return false
    if (splitType === 'percentage') return Math.abs(sharesSum() - 100) <= 0.01
    return Math.abs(sharesSum() - parsedTotal) <= 0.01
  }

  // ── Form submit ──────────────────────────────────────────────────────────────

  async function handleSave(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSuccess(null)

    const parsedTotalNum = parseFloat(total)
    if (!isNaN(parsedTotalNum) && parsedTotalNum <= 0) {
      setError('Total amount must be greater than zero.')
      return
    }
    if (date && date > today) {
      setError('Date cannot be in the future.')
      return
    }

    setSaving(true)
    try {
      const updated = await updateExpense(id, {
        merchantName: merchantName || undefined,
        merchantAddress: merchantAddress || undefined,
        date: date ? new Date(date).toISOString() : undefined,
        time: time || undefined,
        total: parsedTotalNum || undefined,
        subtotal: subtotal ? parseFloat(subtotal) : undefined,
        taxAmount: taxAmount ? parseFloat(taxAmount) : undefined,
        category: category || undefined,
        tags,
        notes: notes || undefined,
        items: buildItemsPayload(),
      })
      setExpense(updated)
      populateForm(updated)
      setSharesOutOfSync(false)
      setSuccess('Expense saved.')
      setTimeout(() => setSuccess(null), 3000)
    } catch (err) {
      if (err instanceof Error && err.message === 'shares_out_of_sync') {
        setSharesOutOfSync(true)
        setError('Total changed — shares need to be re-split before saving.')
      } else {
        setError(err instanceof Error ? err.message : 'Failed to save expense.')
      }
    } finally {
      setSaving(false)
    }
  }

  async function handleConfirmCorrections() {
    setError(null)
    setSuccess(null)
    setConfirming(true)
    try {
      const updated = await correctExpense(id, {
        merchantName: merchantName || undefined,
        date: date ? new Date(date).toISOString() : undefined,
        total: parsedTotal || undefined,
        subtotal: subtotal ? parseFloat(subtotal) : undefined,
        taxAmount: taxAmount ? parseFloat(taxAmount) : undefined,
        category: category || undefined,
        tags,
        notes: notes || undefined,
        items: buildItemsPayload(),
      })
      setExpense(updated)
      populateForm(updated)
      setSuccess('OCR corrections confirmed. Expense updated.')
      setTimeout(() => setSuccess(null), 4000)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to confirm corrections.')
    } finally {
      setConfirming(false)
    }
  }

  async function handleDelete() {
    setDeleting(true)
    try {
      await deleteExpense(id)
      router.push('/expenses')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete expense.')
      setShowDeleteDialog(false)
    } finally {
      setDeleting(false)
    }
  }

  async function handleAssignShares() {
    if (!sharesValid()) return
    setAssigningShares(true)
    setError(null)
    try {
      const updated = await assignShares(id, {
        shares: shareRows.map(r => ({
          userId: r.userId,
          ...(splitType === 'percentage'
            ? { percentage: parseFloat(r.value) || 0 }
            : { amount: parseFloat(r.value) || 0 }),
        })),
      })
      setExpense(updated)
      populateForm(updated)
      setSharesOutOfSync(false)
      setSuccess('Shares saved.')
      setTimeout(() => setSuccess(null), 3000)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to assign shares.')
    } finally {
      setAssigningShares(false)
    }
  }

  async function handleAttachReceipt() {
    const rid = attachReceiptId.trim()
    if (!rid) return
    setAttaching(true)
    setError(null)
    try {
      const updated = await attachReceipt(id, rid)
      setExpense(updated)
      setAttachReceiptId('')
      setSuccess('Receipt attached.')
      setTimeout(() => setSuccess(null), 3000)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to attach receipt.')
    } finally {
      setAttaching(false)
    }
  }

  async function handleDetachReceipt(receiptId: string) {
    setError(null)
    try {
      await detachReceipt(id, receiptId)
      setExpense(prev => prev
        ? { ...prev, receipts: prev.receipts.filter(r => r.id !== receiptId) }
        : prev)
      setDetachTarget(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to detach receipt.')
    }
  }

  // US-EXP-03: File attachment handlers
  async function handleFileUpload(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return
    e.target.value = ''
    setUploadingFile(true)
    setError(null)
    try {
      const added = await uploadAttachment(id, file)
      setAttachments(prev => [...prev, added])
      setSuccess('Attachment uploaded.')
      setTimeout(() => setSuccess(null), 3000)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to upload attachment.')
    } finally {
      setUploadingFile(false)
    }
  }

  async function handleDeleteAttachment(attachmentId: string) {
    setError(null)
    try {
      await deleteAttachment(id, attachmentId)
      setAttachments(prev => prev.filter(a => a.id !== attachmentId))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete attachment.')
    }
  }

  // ── Render ───────────────────────────────────────────────────────────────────

  if (loading) return <main className={styles.container}><p className={styles.loadingText}>Loading…</p></main>

  if (!expense) {
    return (
      <main className={styles.container}>
        <Link href="/expenses" className={styles.backLink}>← Back to Expenses</Link>
        <p className={styles.error}>{error ?? 'Expense not found.'}</p>
      </main>
    )
  }

  const isAdmin = session?.role === 'Admin'
  const isReader = session?.role === 'Reader'
  const receipts: ReceiptSummaryResponse[] = expense.receipts
  const existingShares: ExpenseShareResponse[] = expense.shares

  return (
    <main className={styles.container}>
      <Link href="/expenses" className={styles.backLink}>← Back to Expenses</Link>

      <div className={styles.pageHeader}>
        <h1 className={styles.pageTitle}>
          {expense.merchantName ?? 'Expense Detail'}
        </h1>
        <div className={styles.badges}>
          <span className={`${styles.badge} ${expense.source === 'OCR' ? styles.sourceOCR : styles.sourceManual}`}>
            {expense.source}
          </span>
          {expense.isShared && (
            <span className={styles.sharedBadge}>Shared</span>
          )}
        </div>
      </div>

      {hasOcrData && (
        <div className={styles.ocrBanner}>
          <span className={styles.ocrBannerIcon}>⚠</span>
          <div className={styles.ocrBannerText}>
            <strong>OCR data needs review</strong>
            Fields highlighted with low confidence may need correction. Review and click &ldquo;Confirm Expense&rdquo; when done.
          </div>
        </div>
      )}

      {isReader && (
        <div role="status" className={styles.readerBanner}>
          You have view-only access. You can view this expense but cannot make changes.
        </div>
      )}

      {error && <p role="alert" className={styles.error}>{error}</p>}
      {success && <p role="status" className={styles.successMessage}>{success}</p>}

      <form onSubmit={handleSave} noValidate className={styles.form}>
        {/* ── Merchant ── */}
        <div className={styles.section}>
          <h2 className={styles.sectionTitle}>Merchant</h2>
          <div className={styles.fieldRow}>
            <div className={styles.field}>
              <div className={styles.labelRow}>
                <label htmlFor="merchantName" className={styles.label}>Name</label>
                {hasOcrData && <ConfidenceIndicator score={confidence.merchantName} />}
              </div>
              <input
                id="merchantName"
                type="text"
                className={`${styles.input} ${hasOcrData && (confidence.merchantName ?? 100) < 70 ? styles.inputLowConfidence : ''}`}
                value={merchantName}
                onChange={e => setMerchantName(e.target.value)}
                maxLength={200}
              />
            </div>
            <div className={styles.field}>
              <label htmlFor="merchantAddress" className={styles.label}>Address</label>
              <input
                id="merchantAddress"
                type="text"
                className={styles.input}
                value={merchantAddress}
                onChange={e => setMerchantAddress(e.target.value)}
                maxLength={500}
              />
            </div>
          </div>
        </div>

        {/* ── Date & Time ── */}
        <div className={styles.section}>
          <h2 className={styles.sectionTitle}>Date & Time</h2>
          <div className={styles.fieldRow}>
            <div className={styles.field}>
              <div className={styles.labelRow}>
                <label htmlFor="date" className={styles.label}>Date</label>
                {hasOcrData && <ConfidenceIndicator score={confidence.date} />}
              </div>
              <input
                id="date"
                type="date"
                className={`${styles.input} ${hasOcrData && (confidence.date ?? 100) < 70 ? styles.inputLowConfidence : ''}`}
                value={date}
                max={today}
                onChange={e => setDate(e.target.value)}
              />
            </div>
            <div className={styles.field}>
              <label htmlFor="time" className={styles.label}>Time</label>
              <input
                id="time"
                type="time"
                className={styles.input}
                value={time}
                onChange={e => setTime(e.target.value)}
              />
            </div>
          </div>
        </div>

        {/* ── Amounts ── */}
        <div className={styles.section}>
          <h2 className={styles.sectionTitle}>Amounts</h2>
          <div className={styles.fieldRow}>
            <div className={styles.field}>
              <div className={styles.labelRow}>
                <label htmlFor="total" className={styles.label}>Total</label>
                {hasOcrData && <ConfidenceIndicator score={confidence.total} />}
              </div>
              <input
                id="total"
                type="number"
                className={`${styles.input} ${hasOcrData && (confidence.total ?? 100) < 70 ? styles.inputLowConfidence : ''}`}
                value={total}
                onChange={e => setTotal(e.target.value)}
                min="0.01"
                step="0.01"
                placeholder="0.00"
              />
            </div>
            <div className={styles.field}>
              <label htmlFor="subtotal" className={styles.label}>Subtotal</label>
              <input
                id="subtotal"
                type="number"
                className={styles.input}
                value={subtotal}
                onChange={e => setSubtotal(e.target.value)}
                min="0"
                step="0.01"
                placeholder="0.00"
              />
            </div>
            <div className={styles.field}>
              <label htmlFor="taxAmount" className={styles.label}>Tax</label>
              <input
                id="taxAmount"
                type="number"
                className={styles.input}
                value={taxAmount}
                onChange={e => setTaxAmount(e.target.value)}
                min="0"
                step="0.01"
                placeholder="0.00"
              />
            </div>
          </div>
        </div>

        {/* ── Classification ── */}
        <div className={styles.section}>
          <h2 className={styles.sectionTitle}>Classification</h2>
          <div className={styles.fieldRow}>
            <div className={styles.field}>
              <label htmlFor="category" className={styles.label}>Category</label>
              <select
                id="category"
                className={styles.select}
                value={category}
                onChange={e => setCategory(e.target.value)}
              >
                <option value="">— None —</option>
                {CATEGORIES.map(c => <option key={c} value={c}>{c}</option>)}
              </select>
            </div>
          </div>

          <div className={styles.fieldFull} style={{ marginTop: '1rem' }}>
            <label className={styles.label}>Tags</label>
            <div className={styles.tagInputWrapper}>
              {tags.map(tag => (
                <span key={tag} className={styles.tag}>
                  {tag}
                  <button type="button" className={styles.tagRemove} onClick={() => removeTag(tag)} aria-label={`Remove tag ${tag}`}>×</button>
                </span>
              ))}
              <input
                type="text"
                className={styles.tagInput}
                value={pendingTag}
                onChange={e => setPendingTag(e.target.value)}
                onKeyDown={handleTagKeyDown}
                onBlur={addTag}
                placeholder={tags.length === 0 ? 'Type a tag and press Enter' : ''}
                maxLength={50}
              />
            </div>
            <p className={styles.hint}>Press Enter to add a tag</p>
          </div>
        </div>

        {/* ── Notes ── */}
        <div className={styles.section}>
          <h2 className={styles.sectionTitle}>Notes</h2>
          <div className={styles.field}>
            <textarea
              id="notes"
              className={styles.textarea}
              value={notes}
              onChange={e => setNotes(e.target.value)}
              placeholder="Optional notes…"
              maxLength={1000}
            />
          </div>
        </div>

        {/* ── Barcode (US-OCR-04) ── */}
        {(expense.barcode ?? expense.barcodeType) && (
          <div className={styles.section}>
            <h2 className={styles.sectionTitle}>Barcode</h2>
            <div className={styles.fieldRow}>
              {expense.barcodeType && (
                <div className={styles.field}>
                  <p className={styles.label}>Type</p>
                  <p className={styles.readonlyValue}>{expense.barcodeType}</p>
                </div>
              )}
              {expense.barcode && (
                <div className={styles.field}>
                  <p className={styles.label}>Value</p>
                  <p className={styles.readonlyValue}>{expense.barcode}</p>
                </div>
              )}
            </div>
          </div>
        )}

        {/* ── Line Items (US-EXP-06) ── */}
        <div className={styles.section}>
          <div className={styles.sectionHeader} onClick={() => setItemsExpanded(v => !v)}>
            <h2 className={styles.sectionTitle} style={{ margin: 0 }}>Line Items</h2>
            <button type="button" className={styles.collapseToggle} aria-label={itemsExpanded ? 'Collapse' : 'Expand'}>
              {itemsExpanded ? '▲ Collapse' : '▼ Expand'}
            </button>
          </div>

          {itemsExpanded && (
            <>
              {items.length > 0 && (
                <>
                  <table className={styles.itemsTable} style={{ marginTop: '0.75rem' }}>
                    <thead>
                      <tr>
                        <th className={styles.itemsTh}>Item</th>
                        <th className={styles.itemsThNumber}>Qty</th>
                        <th className={styles.itemsThNumber}>Unit Price</th>
                        <th className={styles.itemsThNumber}>Line Total</th>
                        <th className={styles.itemsTh}></th>
                      </tr>
                    </thead>
                    <tbody>
                      {items.map((item, idx) => (
                        <tr key={idx}>
                          <td className={styles.itemsTd}>
                            <input
                              type="text"
                              className={styles.itemInput}
                              value={item.name}
                              onChange={e => updateItem(idx, 'name', e.target.value)}
                              placeholder="Item name"
                            />
                          </td>
                          <td className={styles.itemsTdNumber}>
                            <input
                              type="number"
                              className={styles.itemInput}
                              value={item.quantity}
                              onChange={e => updateItem(idx, 'quantity', e.target.value)}
                              min="0"
                              step="0.001"
                              style={{ width: '5rem', textAlign: 'right' }}
                            />
                          </td>
                          <td className={styles.itemsTdNumber}>
                            <input
                              type="number"
                              className={styles.itemInput}
                              value={item.unitPrice}
                              onChange={e => updateItem(idx, 'unitPrice', e.target.value)}
                              min="0"
                              step="0.01"
                              style={{ width: '6rem', textAlign: 'right' }}
                            />
                          </td>
                          <td className={styles.itemsTdNumber}>
                            ${((parseFloat(item.quantity) || 0) * (parseFloat(item.unitPrice) || 0)).toFixed(2)}
                          </td>
                          <td className={styles.itemsTd}>
                            <button
                              type="button"
                              className={styles.removeItemButton}
                              onClick={() => removeItem(idx)}
                              aria-label={`Remove ${item.name}`}
                            >×</button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>

                  {/* Running total footer */}
                  <div className={styles.itemsFooter}>
                    <span>Items total:</span>
                    <span className={styles.itemsFooterTotal}>${computedItemsTotal.toFixed(2)}</span>
                  </div>
                </>
              )}

              <button type="button" className={styles.addItemButton} onClick={addItem}>
                + Add line item
              </button>

              {showTotalMismatch && (
                <p className={styles.totalWarning}>
                  ⚠ Line items total (${computedItemsTotal.toFixed(2)}) does not match the expense total (${parsedTotal.toFixed(2)}). Save will proceed — update either value to reconcile.
                </p>
              )}
            </>
          )}
        </div>

        {/* ── Shared Expense (US-EXP-04) ── */}
        <div className={styles.section}>
          <div className={styles.sectionHeader} onClick={() => setSharesExpanded(v => !v)}>
            <h2 className={styles.sectionTitle} style={{ margin: 0 }}>Shared Expense</h2>
            <button type="button" className={styles.collapseToggle} aria-label={sharesExpanded ? 'Collapse' : 'Expand'}>
              {sharesExpanded ? '▲ Collapse' : '▼ Expand'}
            </button>
          </div>

          {sharesExpanded && (
            <>
              <div className={styles.sharedToggleRow} style={{ marginTop: '0.875rem' }}>
                <label className={styles.toggleSwitch}>
                  <input
                    type="checkbox"
                    checked={isShared}
                    onChange={e => {
                      setIsShared(e.target.checked)
                      if (!e.target.checked) setShareRows([])
                    }}
                  />
                  <span className={styles.toggleSlider} />
                </label>
                <span className={styles.toggleLabel}>
                  {isShared ? 'Split this expense with household members' : 'This is a personal expense'}
                </span>
              </div>

              {isShared && (
                <>
                  <div className={styles.splitTypeRow}>
                    <label className={styles.splitTypeLabel}>
                      <input
                        type="radio"
                        name="splitType"
                        value="percentage"
                        checked={splitType === 'percentage'}
                        onChange={() => setSplitType('percentage')}
                      />
                      By percentage
                    </label>
                    <label className={styles.splitTypeLabel}>
                      <input
                        type="radio"
                        name="splitType"
                        value="amount"
                        checked={splitType === 'amount'}
                        onChange={() => setSplitType('amount')}
                      />
                      By amount
                    </label>
                  </div>

                  {shareRows.length > 0 && (
                    <table className={styles.sharesTable}>
                      <thead>
                        <tr>
                          <th className={styles.sharesTh}>Member (User ID)</th>
                          <th className={styles.sharesTh}>{splitType === 'percentage' ? '%' : '$'}</th>
                          <th className={styles.sharesTh}></th>
                        </tr>
                      </thead>
                      <tbody>
                        {shareRows.map((row, idx) => (
                          <tr key={idx}>
                            <td className={styles.sharesTd}>
                              <input
                                type="text"
                                className={styles.sharesInput}
                                style={{ maxWidth: '100%', textAlign: 'left' }}
                                value={row.userId}
                                onChange={e => setShareRows(prev => prev.map((r, i) => i === idx ? { ...r, userId: e.target.value } : r))}
                                placeholder="User ID"
                              />
                            </td>
                            <td className={styles.sharesTd}>
                              <input
                                type="number"
                                className={styles.sharesInput}
                                value={row.value}
                                onChange={e => updateShareRow(idx, e.target.value)}
                                min="0"
                                step={splitType === 'percentage' ? '0.1' : '0.01'}
                                placeholder={splitType === 'percentage' ? '0.0' : '0.00'}
                              />
                            </td>
                            <td className={styles.sharesTd}>
                              <button type="button" className={styles.removeItemButton} onClick={() => removeShareRow(idx)}>×</button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}

                  <button type="button" className={styles.addItemButton} onClick={addShareRow} style={{ marginTop: '0.5rem' }}>
                    + Add member
                  </button>

                  <div className={styles.sharesSumRow}>
                    <span>Total split:</span>
                    <span className={sharesValid() ? styles.itemsFooterTotal : styles.sharesSumBad}>
                      {splitType === 'percentage'
                        ? `${sharesSum().toFixed(1)}% / 100%`
                        : `$${sharesSum().toFixed(2)} / $${parsedTotal.toFixed(2)}`}
                    </span>
                  </div>

                  {sharesOutOfSync && (
                    <div className={styles.sharesOutOfSyncBanner}>
                      ⚠ The expense total changed. Update the split amounts below to match the new total, then save.
                    </div>
                  )}

                  <button
                    type="button"
                    className={styles.assignSharesButton}
                    disabled={!sharesValid() || assigningShares || shareRows.length === 0}
                    onClick={handleAssignShares}
                  >
                    {assigningShares ? 'Saving shares…' : 'Save Shares'}
                  </button>

                  {existingShares.length > 0 && (
                    <p className={styles.detachConfirmBanner}>
                      {existingShares.length} share{existingShares.length !== 1 ? 's' : ''} currently saved.
                    </p>
                  )}
                </>
              )}
            </>
          )}
        </div>

        {/* ── Receipts Gallery (US-REC-03) ── */}
        {(receipts.length > 0 || isAdmin) && (
          <div className={styles.section}>
            <div className={styles.sectionHeader} onClick={() => setReceiptsExpanded(v => !v)}>
              <h2 className={styles.sectionTitle} style={{ margin: 0 }}>
                Receipts {receipts.length > 0 && `(${receipts.length})`}
              </h2>
              <button type="button" className={styles.collapseToggle} aria-label={receiptsExpanded ? 'Collapse' : 'Expand'}>
                {receiptsExpanded ? '▲ Collapse' : '▼ Expand'}
              </button>
            </div>

            {receiptsExpanded && (
              <>
                {receipts.length > 0 && (
                  <div className={styles.receiptGallery}>
                    {receipts.slice(0, 5).map(r => (
                      <div key={r.id} className={styles.receiptThumbCard}>
                        {r.thumbnailUrl ? (
                          <img
                            src={toAbsoluteUrl(r.thumbnailUrl)}
                            alt="Receipt thumbnail"
                            className={styles.receiptThumb}
                          />
                        ) : (
                          <div className={styles.receiptThumbPlaceholder}>No thumbnail</div>
                        )}
                        {detachTarget === r.id ? (
                          <button
                            type="button"
                            className={styles.receiptThumbRemove}
                            onClick={() => void handleDetachReceipt(r.id)}
                            title="Confirm detach"
                          >✓</button>
                        ) : (
                          // Don't allow detaching the primary receipt
                          expense.receiptId !== r.id && (
                            <button
                              type="button"
                              className={styles.receiptThumbRemove}
                              onClick={() => setDetachTarget(prev => prev === r.id ? null : r.id)}
                              title="Detach receipt"
                            >×</button>
                          )
                        )}
                        <p className={styles.receiptThumbStatus}>{r.status}</p>
                      </div>
                    ))}
                    {receipts.length > 5 && (
                      <div className={styles.receiptThumbPlaceholder}>
                        +{receipts.length - 5} more
                      </div>
                    )}
                  </div>
                )}

                <div className={styles.attachReceiptSection}>
                  <input
                    type="text"
                    className={styles.attachReceiptInput}
                    value={attachReceiptId}
                    onChange={e => setAttachReceiptId(e.target.value)}
                    placeholder="Receipt ID to attach…"
                  />
                  <button
                    type="button"
                    className={styles.attachReceiptButton}
                    disabled={!attachReceiptId.trim() || attaching}
                    onClick={handleAttachReceipt}
                  >
                    {attaching ? 'Attaching…' : 'Attach Receipt'}
                  </button>
                </div>
              </>
            )}
          </div>
        )}

        {/* ── File Attachments (US-EXP-03) ── */}
        <div className={styles.section}>
          <div className={styles.sectionHeader} onClick={() => setAttachmentsExpanded(v => !v)}>
            <h2 className={styles.sectionTitle} style={{ margin: 0 }}>
              Attachments {attachments.length > 0 && `(${attachments.length})`}
            </h2>
            <button type="button" className={styles.collapseToggle} aria-label={attachmentsExpanded ? 'Collapse' : 'Expand'}>
              {attachmentsExpanded ? '▲ Collapse' : '▼ Expand'}
            </button>
          </div>

          {attachmentsExpanded && (
            <>
              {attachments.length > 0 && (
                <ul className={styles.attachmentList}>
                  {attachments.map(a => (
                    <li key={a.id} className={styles.attachmentItem}>
                      <span className={styles.attachmentName} title={a.fileName}>{a.fileName}</span>
                      <span className={styles.attachmentMeta}>
                        {(a.fileSizeBytes / 1024).toFixed(0)} KB
                      </span>
                      {!isReader && (
                        <button
                          type="button"
                          className={styles.removeItemButton}
                          onClick={() => void handleDeleteAttachment(a.id)}
                          aria-label={`Delete ${a.fileName}`}
                        >×</button>
                      )}
                    </li>
                  ))}
                </ul>
              )}

              {!isReader && (
                <div style={{ marginTop: '0.75rem' }}>
                  <input
                    ref={fileInputRef}
                    type="file"
                    className={styles.hiddenInput}
                    onChange={handleFileUpload}
                    accept=".jpg,.jpeg,.png,.pdf,.txt,.doc,.docx"
                    aria-hidden="true"
                  />
                  <button
                    type="button"
                    className={styles.addItemButton}
                    disabled={uploadingFile}
                    onClick={() => fileInputRef.current?.click()}
                  >
                    {uploadingFile ? 'Uploading…' : '+ Upload attachment'}
                  </button>
                </div>
              )}
            </>
          )}
        </div>

        {/* ── Form actions ── */}
        {!isReader && (
          <div className={styles.formActions}>
            <button
              type="button"
              className={styles.deleteButton}
              onClick={() => setShowDeleteDialog(true)}
            >
              Delete
            </button>
            <div className={styles.formActionsRight}>
              {hasOcrData && (
                <button
                  type="button"
                  className={styles.confirmButton}
                  disabled={confirming || saving}
                  onClick={handleConfirmCorrections}
                >
                  {confirming ? 'Confirming…' : 'Confirm Expense'}
                </button>
              )}
              <button
                type="submit"
                className={styles.primaryButton}
                disabled={saving || confirming}
              >
                {saving ? 'Saving…' : 'Save Changes'}
              </button>
            </div>
          </div>
        )}
      </form>

      {/* ── Delete dialog ── */}
      {showDeleteDialog && (
        <div className={styles.dialogOverlay} role="dialog" aria-modal="true" aria-labelledby="delete-dialog-title">
          <div className={styles.dialog}>
            <h2 id="delete-dialog-title" className={styles.dialogTitle}>Delete Expense?</h2>
            <p className={styles.dialogBody}>
              This will permanently delete this expense. This action cannot be undone.
            </p>
            <div className={styles.dialogActions}>
              <button
                className={styles.cancelButton}
                disabled={deleting}
                onClick={() => setShowDeleteDialog(false)}
              >
                Cancel
              </button>
              <button
                className={styles.deleteButton}
                disabled={deleting}
                onClick={handleDelete}
              >
                {deleting ? 'Deleting…' : 'Delete'}
              </button>
            </div>
          </div>
        </div>
      )}
    </main>
  )
}
