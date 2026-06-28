'use client'

import { useEffect, useState } from 'react'
import { createBudget, deleteBudget, getBudgets, updateBudget } from '@/api/budgets'
import type { BudgetResponse, CreateBudgetRequest } from '@/api/types'
import styles from './budgets.module.css'

const CATEGORIES = ['Groceries', 'Dining', 'Utilities', 'Transport', 'Health', 'Other']

function formatAmount(amount: number): string {
  return `$${amount.toFixed(2)}`
}

function progressClass(pct: number): string {
  if (pct >= 100) return styles.progressRed
  if (pct >= 80) return styles.progressAmber
  return styles.progressGreen
}

type EditState = { id: string; monthlyLimit: string } | null

export default function BudgetsPage() {
  const [budgets, setBudgets] = useState<BudgetResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  // Category budget create form
  const [newCategory, setNewCategory] = useState('')
  const [newLimit, setNewLimit] = useState('')
  const [creating, setCreating] = useState(false)

  // Household budget create form
  const [newHouseholdLimit, setNewHouseholdLimit] = useState('')
  const [creatingHousehold, setCreatingHousehold] = useState(false)

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

  function flash(msg: string) {
    setSuccess(msg)
    setTimeout(() => setSuccess(null), 3000)
  }

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault()
    const limit = parseFloat(newLimit)
    if (!newCategory || isNaN(limit) || limit <= 0) {
      setError('Please select a category and enter a valid monthly limit.')
      return
    }
    const usedCategories = categoryBudgets.map(b => b.category)
    if (usedCategories.includes(newCategory)) {
      setError(`A budget for "${newCategory}" already exists.`)
      return
    }
    setCreating(true)
    setError(null)
    try {
      const body: CreateBudgetRequest = { category: newCategory, monthlyLimit: limit, type: 'category' }
      const created = await createBudget(body)
      setBudgets(prev => [...prev, created].sort((a, b) => a.category.localeCompare(b.category)))
      setNewCategory('')
      setNewLimit('')
      flash('Budget created.')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create budget.')
    } finally {
      setCreating(false)
    }
  }

  async function handleCreateHousehold(e: React.FormEvent) {
    e.preventDefault()
    const limit = parseFloat(newHouseholdLimit)
    if (isNaN(limit) || limit <= 0) {
      setError('Enter a valid monthly limit for the household budget.')
      return
    }
    setCreatingHousehold(true)
    setError(null)
    try {
      const body: CreateBudgetRequest = { category: 'household', monthlyLimit: limit, type: 'household' }
      const created = await createBudget(body)
      setBudgets(prev => [...prev, created])
      setNewHouseholdLimit('')
      flash('Household budget created.')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create household budget.')
    } finally {
      setCreatingHousehold(false)
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
      flash('Budget updated.')
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
      flash('Budget deleted.')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete budget.')
    } finally {
      setDeleting(false)
    }
  }

  const categoryBudgets = budgets.filter(b => b.type === 'category')
  const householdBudgets = budgets.filter(b => b.type === 'household')
  const usedCategories = categoryBudgets.map(b => b.category)
  const availableCategories = CATEGORIES.filter(c => !usedCategories.includes(c))
  const hasHouseholdBudget = householdBudgets.length > 0

  return (
    <main className={styles.container}>
      <h1 className={styles.pageTitle}>Budgets</h1>
      <p className={styles.pageDescription}>
        Set monthly spending limits per category or for the whole household.
      </p>

      {error && <p role="alert" className={styles.error}>{error}</p>}
      {success && <p role="status" className={styles.success}>{success}</p>}

      {/* Household budget section */}
      <section className={styles.householdSection} aria-label="Household budget">
        <h2 className={styles.sectionTitle}>Household Budget</h2>

        {householdBudgets.map(b => (
          <div key={b.id} className={styles.householdCard}>
            {b.progressPercent >= 80 && (
              <p className={styles.alertBanner}>
                {b.progressPercent >= 100
                  ? `Household budget exceeded (${b.progressPercent.toFixed(0)}%).`
                  : `Household budget at ${b.progressPercent.toFixed(0)}% — approaching the limit.`}
              </p>
            )}
            <div className={styles.householdHeader}>
              <span className={styles.householdLabel}>Monthly Limit</span>
              <span className={styles.householdAmount}>{formatAmount(b.spent)} / {formatAmount(b.monthlyLimit)}</span>
              <span className={styles.householdPct}>{b.progressPercent.toFixed(0)}%</span>
            </div>
            <div className={styles.progressTrack}>
              <div
                className={`${styles.progressFill} ${progressClass(b.progressPercent)}`}
                style={{ width: `${Math.min(b.progressPercent, 100)}%` }}
              />
            </div>
            {b.memberBreakdown && b.memberBreakdown.length > 0 && (
              <div className={styles.memberBreakdown}>
                <p className={styles.memberBreakdownTitle}>Member Breakdown</p>
                <ul className={styles.memberList} role="list">
                  {b.memberBreakdown.map(m => (
                    <li key={m.userId} className={styles.memberItem}>
                      <span>{m.displayName}</span>
                      <span>{formatAmount(m.contributed)}</span>
                    </li>
                  ))}
                </ul>
              </div>
            )}
            <div className={styles.rowActions} style={{ marginTop: '0.75rem' }}>
              {editState?.id === b.id ? (
                <>
                  <input
                    type="number"
                    className={styles.editInput}
                    value={editState.monthlyLimit}
                    onChange={e => setEditState({ ...editState, monthlyLimit: e.target.value })}
                    min="0.01"
                    step="0.01"
                    autoFocus
                  />
                  <button className={styles.saveButton} disabled={saving} onClick={() => void handleUpdate(b.id)}>
                    {saving ? 'Saving…' : 'Save'}
                  </button>
                  <button className={styles.cancelButton} disabled={saving} onClick={() => setEditState(null)}>
                    Cancel
                  </button>
                </>
              ) : deleteTarget === b.id ? (
                <>
                  <span className={styles.confirmText}>Delete household budget?</span>
                  <button className={styles.confirmYes} disabled={deleting} onClick={() => void handleDelete(b.id)}>Yes</button>
                  <button className={styles.confirmNo} disabled={deleting} onClick={() => setDeleteTarget(null)}>No</button>
                </>
              ) : (
                <>
                  <button className={styles.editButton} onClick={() => setEditState({ id: b.id, monthlyLimit: String(b.monthlyLimit) })}>Edit</button>
                  <button className={styles.deleteButton} onClick={() => setDeleteTarget(b.id)}>Delete</button>
                </>
              )}
            </div>
          </div>
        ))}

        {!hasHouseholdBudget && (
          <form onSubmit={handleCreateHousehold} className={styles.addHouseholdForm} noValidate>
            <div className={styles.createField}>
              <label htmlFor="householdLimit" className={styles.label}>Monthly Limit</label>
              <input
                id="householdLimit"
                type="number"
                className={styles.input}
                value={newHouseholdLimit}
                onChange={e => setNewHouseholdLimit(e.target.value)}
                min="0.01"
                step="0.01"
                placeholder="0.00"
                required
              />
            </div>
            <button type="submit" className={styles.primaryButton} disabled={creatingHousehold}>
              {creatingHousehold ? 'Creating…' : 'Add Household Budget'}
            </button>
          </form>
        )}
      </section>

      {/* Category budgets */}
      <section aria-label="Category budgets">
        <h2 className={styles.sectionTitle}>Category Budgets</h2>
        <p className={styles.pageDescription}>Set a limit per spending category.</p>

        {/* Create form */}
        <div className={styles.createSection} aria-label="Add category budget">
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
        </div>

        {/* Budget list */}
        {loading ? (
          <p className={styles.loadingText}>Loading…</p>
        ) : categoryBudgets.length === 0 ? (
          <p className={styles.emptyText}>No category budgets set yet. Add one above.</p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th className={styles.th}>Category</th>
                <th className={styles.th}>Progress</th>
                <th className={styles.th}>Limit</th>
                <th className={styles.th}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {categoryBudgets.map(b => (
                <tr key={b.id} className={styles.tr}>
                  <td className={styles.td}>
                    <span className={styles.categoryLabel}>{b.category}</span>
                  </td>
                  <td className={`${styles.td} ${styles.tdProgress}`}>
                    {b.progressPercent >= 80 && (
                      <p className={styles.alertBanner}>
                        {b.progressPercent >= 100 ? 'Exceeded' : `${b.progressPercent.toFixed(0)}% used`}
                      </p>
                    )}
                    <div className={styles.progressStack}>
                      <div className={styles.progressLabel}>
                        <span>{formatAmount(b.spent)}</span>
                        <span>{b.progressPercent.toFixed(0)}%</span>
                      </div>
                      <div className={styles.progressTrack}>
                        <div
                          className={`${styles.progressFill} ${progressClass(b.progressPercent)}`}
                          style={{ width: `${Math.min(b.progressPercent, 100)}%` }}
                        />
                      </div>
                    </div>
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
                          <button className={styles.saveButton} disabled={saving} onClick={() => void handleUpdate(b.id)}>
                            {saving ? 'Saving…' : 'Save'}
                          </button>
                          <button className={styles.cancelButton} disabled={saving} onClick={() => setEditState(null)}>
                            Cancel
                          </button>
                        </>
                      ) : deleteTarget === b.id ? (
                        <>
                          <span className={styles.confirmText}>Delete?</span>
                          <button className={styles.confirmYes} disabled={deleting} onClick={() => void handleDelete(b.id)}>Yes</button>
                          <button className={styles.confirmNo} disabled={deleting} onClick={() => setDeleteTarget(null)}>No</button>
                        </>
                      ) : (
                        <>
                          <button className={styles.editButton} onClick={() => setEditState({ id: b.id, monthlyLimit: String(b.monthlyLimit) })}>Edit</button>
                          <button className={styles.deleteButton} onClick={() => setDeleteTarget(b.id)}>Delete</button>
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
