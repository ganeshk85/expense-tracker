'use client'

import { useEffect, useState } from 'react'
import { createBudget, deleteBudget, getBudgets, updateBudget } from '@/api/budgets'
import type { BudgetResponse, CreateBudgetRequest } from '@/api/types'
import styles from './budgets.module.css'

const CATEGORIES = ['Groceries', 'Dining', 'Utilities', 'Transport', 'Health', 'Other']

function formatAmount(amount: number): string {
  return `$${amount.toFixed(2)}`
}

type EditState = { id: string; monthlyLimit: string } | null

export default function BudgetsPage() {
  const [budgets, setBudgets] = useState<BudgetResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  // Create form state
  const [newCategory, setNewCategory] = useState('')
  const [newLimit, setNewLimit] = useState('')
  const [creating, setCreating] = useState(false)

  // Edit state
  const [editState, setEditState] = useState<EditState>(null)
  const [saving, setSaving] = useState(false)

  // Delete confirm
  const [deleteTarget, setDeleteTarget] = useState<string | null>(null)
  const [deleting, setDeleting] = useState(false)

  useEffect(() => {
    void load()
  }, [])

  async function load() {
    try {
      const res = await getBudgets()
      setBudgets(res.items)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load budgets.')
    } finally {
      setLoading(false)
    }
  }

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault()
    const limit = parseFloat(newLimit)
    if (!newCategory || isNaN(limit) || limit <= 0) {
      setError('Please select a category and enter a valid monthly limit.')
      return
    }
    const usedCategories = budgets.map(b => b.category)
    if (usedCategories.includes(newCategory)) {
      setError(`A budget for "${newCategory}" already exists.`)
      return
    }
    setCreating(true)
    setError(null)
    try {
      const body: CreateBudgetRequest = { category: newCategory, monthlyLimit: limit }
      const created = await createBudget(body)
      setBudgets(prev => [...prev, created].sort((a, b) => a.category.localeCompare(b.category)))
      setNewCategory('')
      setNewLimit('')
      setSuccess('Budget created.')
      setTimeout(() => setSuccess(null), 3000)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create budget.')
    } finally {
      setCreating(false)
    }
  }

  async function handleUpdate(id: string) {
    if (!editState) return
    const limit = parseFloat(editState.monthlyLimit)
    if (isNaN(limit) || limit <= 0) {
      setError('Monthly limit must be greater than zero.')
      return
    }
    setSaving(true)
    setError(null)
    try {
      const updated = await updateBudget(id, { monthlyLimit: limit })
      setBudgets(prev => prev.map(b => b.id === id ? updated : b))
      setEditState(null)
      setSuccess('Budget updated.')
      setTimeout(() => setSuccess(null), 3000)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update budget.')
    } finally {
      setSaving(false)
    }
  }

  async function handleDelete(id: string) {
    setDeleting(true)
    setError(null)
    try {
      await deleteBudget(id)
      setBudgets(prev => prev.filter(b => b.id !== id))
      setDeleteTarget(null)
      setSuccess('Budget deleted.')
      setTimeout(() => setSuccess(null), 3000)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete budget.')
    } finally {
      setDeleting(false)
    }
  }

  const usedCategories = budgets.map(b => b.category)
  const availableCategories = CATEGORIES.filter(c => !usedCategories.includes(c))

  return (
    <main className={styles.container}>
      <h1 className={styles.pageTitle}>Category Budgets</h1>
      <p className={styles.pageDescription}>
        Set monthly spending limits per category. You will be alerted when spending approaches the limit.
      </p>

      {error && <p role="alert" className={styles.error}>{error}</p>}
      {success && <p role="status" className={styles.success}>{success}</p>}

      {/* Create form */}
      <section className={styles.createSection} aria-label="Add budget">
        <h2 className={styles.sectionTitle}>Add Budget</h2>
        <form onSubmit={handleCreate} className={styles.createForm} noValidate>
          <div className={styles.createField}>
            <label htmlFor="newCategory" className={styles.label}>Category</label>
            <select
              id="newCategory"
              className={styles.select}
              value={newCategory}
              onChange={e => setNewCategory(e.target.value)}
              required
            >
              <option value="">— Select —</option>
              {availableCategories.map(c => (
                <option key={c} value={c}>{c}</option>
              ))}
            </select>
          </div>
          <div className={styles.createField}>
            <label htmlFor="newLimit" className={styles.label}>Monthly Limit</label>
            <input
              id="newLimit"
              type="number"
              className={styles.input}
              value={newLimit}
              onChange={e => setNewLimit(e.target.value)}
              min="0.01"
              step="0.01"
              placeholder="0.00"
              required
            />
          </div>
          <button type="submit" className={styles.primaryButton} disabled={creating || availableCategories.length === 0}>
            {creating ? 'Adding…' : 'Add Budget'}
          </button>
        </form>
        {availableCategories.length === 0 && (
          <p className={styles.hint}>All categories have a budget set.</p>
        )}
      </section>

      {/* Budget list */}
      <section aria-label="Budget list">
        <h2 className={styles.sectionTitle}>Current Budgets</h2>
        {loading ? (
          <p className={styles.loadingText}>Loading…</p>
        ) : budgets.length === 0 ? (
          <p className={styles.emptyText}>No budgets set yet. Add one above.</p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th className={styles.th}>Category</th>
                <th className={styles.th}>Monthly Limit</th>
                <th className={styles.th}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {budgets.map(b => (
                <tr key={b.id} className={styles.tr}>
                  <td className={styles.td}>
                    <span className={styles.categoryLabel}>{b.category}</span>
                  </td>
                  <td className={styles.td}>
                    {editState?.id === b.id ? (
                      <input
                        type="number"
                        className={styles.editInput}
                        value={editState.monthlyLimit}
                        onChange={e => setEditState({ ...editState, monthlyLimit: e.target.value })}
                        min="0.01"
                        step="0.01"
                        autoFocus
                      />
                    ) : (
                      <span className={styles.limitValue}>{formatAmount(b.monthlyLimit)}</span>
                    )}
                  </td>
                  <td className={styles.td}>
                    <div className={styles.rowActions}>
                      {editState?.id === b.id ? (
                        <>
                          <button
                            className={styles.saveButton}
                            disabled={saving}
                            onClick={() => void handleUpdate(b.id)}
                          >
                            {saving ? 'Saving…' : 'Save'}
                          </button>
                          <button
                            className={styles.cancelButton}
                            disabled={saving}
                            onClick={() => setEditState(null)}
                          >
                            Cancel
                          </button>
                        </>
                      ) : deleteTarget === b.id ? (
                        <>
                          <span className={styles.confirmText}>Delete?</span>
                          <button
                            className={styles.confirmYes}
                            disabled={deleting}
                            onClick={() => void handleDelete(b.id)}
                          >
                            Yes
                          </button>
                          <button
                            className={styles.confirmNo}
                            disabled={deleting}
                            onClick={() => setDeleteTarget(null)}
                          >
                            No
                          </button>
                        </>
                      ) : (
                        <>
                          <button
                            className={styles.editButton}
                            onClick={() => setEditState({ id: b.id, monthlyLimit: String(b.monthlyLimit) })}
                          >
                            Edit
                          </button>
                          <button
                            className={styles.deleteButton}
                            onClick={() => setDeleteTarget(b.id)}
                          >
                            Delete
                          </button>
                        </>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </main>
  )
}
