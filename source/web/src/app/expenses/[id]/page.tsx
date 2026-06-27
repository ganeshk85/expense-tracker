'use client'

import { use, useEffect, useState, KeyboardEvent } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import {
  correctExpense,
  deleteExpense,
  getExpense,
  updateExpense,
} from '@/api/expenses'
import type { ExpenseResponse } from '@/api/types'
import styles from './expense-detail.module.css'

const CATEGORIES = ['Groceries', 'Dining', 'Utilities', 'Transport', 'Health', 'Other']

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

export default function ExpenseDetailPage({
  params,
}: {
  params: Promise<{ id: string }>
}) {
  const { id } = use(params)
  const router = useRouter()

  const [expense, setExpense] = useState<ExpenseResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [showDeleteDialog, setShowDeleteDialog] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [confirming, setConfirming] = useState(false)

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
  }

  useEffect(() => {
    async function load() {
      try {
        const e = await getExpense(id)
        setExpense(e)
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

  // Show total mismatch warning if items exist and total diverges
  const computedItemsTotal = itemsTotal(items)
  const parsedTotal = parseFloat(total) || 0
  const showTotalMismatch =
    items.length > 0 &&
    parsedTotal > 0 &&
    Math.abs(computedItemsTotal - parsedTotal) > 0.01

  // Tag helpers
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

  // Item helpers
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
      setSuccess('Expense saved.')
      setTimeout(() => setSuccess(null), 3000)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save expense.')
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

  if (loading) return <main className={styles.container}><p className={styles.loadingText}>Loading…</p></main>

  if (!expense) {
    return (
      <main className={styles.container}>
        <Link href="/expenses" className={styles.backLink}>← Back to Expenses</Link>
        <p className={styles.error}>{error ?? 'Expense not found.'}</p>
      </main>
    )
  }

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
        </div>
      </div>

      {hasOcrData && (
        <div className={styles.ocrBanner}>
          <span className={styles.ocrBannerIcon}>⚠</span>
          <div className={styles.ocrBannerText}>
            <strong>OCR data needs review</strong>
            Fields highlighted with low confidence may need correction. Review and click "Confirm Expense" when done.
          </div>
        </div>
      )}

      {error && <p role="alert" className={styles.error}>{error}</p>}
      {success && <p role="status" className={styles.successMessage}>{success}</p>}

      <form onSubmit={handleSave} noValidate className={styles.form}>
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

        {(items.length > 0 || expense.source === 'OCR') && (
          <div className={styles.section}>
            <h2 className={styles.sectionTitle}>Line Items</h2>
            {items.length > 0 && (
              <table className={styles.itemsTable}>
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
                        >
                          ×
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}

            <button type="button" className={styles.addItemButton} onClick={addItem}>
              + Add line item
            </button>

            {showTotalMismatch && (
              <p className={styles.totalWarning}>
                ⚠ Line items total (${computedItemsTotal.toFixed(2)}) does not match the total amount (${parsedTotal.toFixed(2)}).
              </p>
            )}
          </div>
        )}

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
      </form>

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
