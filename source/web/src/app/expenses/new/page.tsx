'use client'

import { useState, KeyboardEvent } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { createExpense } from '@/api/expenses'
import styles from './new.module.css'

const CATEGORIES = ['Groceries', 'Dining', 'Utilities', 'Transport', 'Health', 'Other']

export default function NewExpensePage() {
  const router = useRouter()
  const [merchantName, setMerchantName] = useState('')
  const [date, setDate] = useState('')
  const [total, setTotal] = useState('')
  const [category, setCategory] = useState('')
  const [tags, setTags] = useState<string[]>([])
  const [pendingTag, setPendingTag] = useState('')
  const [notes, setNotes] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const today = new Date().toISOString().split('T').at(0) ?? ''

  function addTag() {
    const trimmed = pendingTag.trim()
    if (trimmed && !tags.includes(trimmed)) {
      setTags(prev => [...prev, trimmed])
    }
    setPendingTag('')
  }

  function removeTag(tag: string) {
    setTags(prev => prev.filter(t => t !== tag))
  }

  function handleTagKeyDown(e: KeyboardEvent<HTMLInputElement>) {
    if (e.key === 'Enter') {
      e.preventDefault()
      addTag()
    } else if (e.key === 'Backspace' && pendingTag === '' && tags.length > 0) {
      setTags(prev => prev.slice(0, -1))
    }
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)

    const parsedTotal = parseFloat(total)
    if (isNaN(parsedTotal) || parsedTotal <= 0) {
      setError('Total amount must be greater than zero.')
      return
    }
    if (date && date > today) {
      setError('Date cannot be in the future.')
      return
    }

    setSaving(true)
    try {
      const expense = await createExpense({
        merchantName: merchantName || undefined,
        date: date ? new Date(date).toISOString() : undefined,
        total: parsedTotal,
        category: category || undefined,
        tags: tags.length > 0 ? tags : undefined,
        notes: notes || undefined,
      })
      router.push(`/expenses/${expense.id}`)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create expense.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <main className={styles.container}>
      <Link href="/expenses" className={styles.backLink}>
        ← Back to Expenses
      </Link>

      <h1 className={styles.pageTitle}>New Expense</h1>

      <form onSubmit={handleSubmit} noValidate className={styles.form}>
        <div className={styles.field}>
          <label htmlFor="merchantName" className={styles.label}>Merchant Name</label>
          <input
            id="merchantName"
            type="text"
            className={styles.input}
            value={merchantName}
            onChange={e => setMerchantName(e.target.value)}
            placeholder="e.g. Whole Foods"
            maxLength={200}
          />
        </div>

        <div className={styles.field}>
          <label htmlFor="date" className={styles.label}>Date</label>
          <input
            id="date"
            type="date"
            className={styles.input}
            value={date}
            max={today}
            onChange={e => setDate(e.target.value)}
          />
        </div>

        <div className={styles.field}>
          <label htmlFor="total" className={styles.label}>
            Total Amount<span className={styles.required}>*</span>
          </label>
          <input
            id="total"
            type="number"
            className={styles.input}
            value={total}
            onChange={e => setTotal(e.target.value)}
            min="0.01"
            step="0.01"
            placeholder="0.00"
            required
          />
        </div>

        <div className={styles.field}>
          <label htmlFor="category" className={styles.label}>Category</label>
          <select
            id="category"
            className={styles.select}
            value={category}
            onChange={e => setCategory(e.target.value)}
          >
            <option value="">— Select category —</option>
            {CATEGORIES.map(c => (
              <option key={c} value={c}>{c}</option>
            ))}
          </select>
        </div>

        <div className={styles.field}>
          <label className={styles.label}>Tags</label>
          <div className={styles.tagInputWrapper}>
            {tags.map(tag => (
              <span key={tag} className={styles.tag}>
                {tag}
                <button
                  type="button"
                  className={styles.tagRemove}
                  onClick={() => removeTag(tag)}
                  aria-label={`Remove tag ${tag}`}
                >
                  ×
                </button>
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

        <div className={styles.field}>
          <label htmlFor="notes" className={styles.label}>Notes</label>
          <textarea
            id="notes"
            className={styles.textarea}
            value={notes}
            onChange={e => setNotes(e.target.value)}
            placeholder="Optional notes…"
            maxLength={1000}
          />
        </div>

        {error && <p role="alert" className={styles.error}>{error}</p>}

        <div className={styles.formActions}>
          <Link href="/expenses" className={styles.cancelButton}>
            Cancel
          </Link>
          <button
            type="submit"
            className={styles.primaryButton}
            disabled={saving || !total}
          >
            {saving ? 'Saving…' : 'Create Expense'}
          </button>
        </div>
      </form>
    </main>
  )
}
