'use client'

import { useEffect, useState } from 'react'
import { useRouter } from 'next/navigation'
import { getSession } from '@/api/expenses'
import { createAlias, deleteAlias, getAliases, getIntelligenceSummary } from '@/api/intelligence'
import type { IntelligenceSummaryResponse, MerchantAliasEntry, SessionResponse } from '@/api/types'
import styles from './intelligence-settings.module.css'

export default function IntelligenceSettingsPage() {
  const router = useRouter()
  const [session, setSession] = useState<SessionResponse | null>(null)
  const [checkingAccess, setCheckingAccess] = useState(true)

  const [summary, setSummary] = useState<IntelligenceSummaryResponse | null>(null)
  const [aliases, setAliases] = useState<MerchantAliasEntry[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [aliasInput, setAliasInput] = useState('')
  const [canonicalInput, setCanonicalInput] = useState('')
  const [adding, setAdding] = useState(false)
  const [deletingId, setDeletingId] = useState<string | null>(null)

  useEffect(() => {
    async function checkAccess() {
      try {
        const s = await getSession()
        setSession(s)
        if (s.role !== 'Admin') {
          router.replace('/dashboard')
          return
        }
        await loadData()
      } catch {
        router.replace('/dashboard')
      } finally {
        setCheckingAccess(false)
      }
    }
    void checkAccess()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  async function loadData() {
    setLoading(true)
    setError(null)
    try {
      const [summaryRes, aliasesRes] = await Promise.all([getIntelligenceSummary(), getAliases()])
      setSummary(summaryRes)
      setAliases(aliasesRes.items)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load intelligence settings.')
    } finally {
      setLoading(false)
    }
  }

  async function handleAddAlias(e: React.FormEvent) {
    e.preventDefault()
    setError(null)

    const alias = aliasInput.trim()
    const canonical = canonicalInput.trim()
    if (!alias || !canonical) {
      setError('Both alias and canonical name are required.')
      return
    }
    if (alias.toLowerCase() === canonical.toLowerCase()) {
      setError('Alias and canonical name must not be the same.')
      return
    }

    setAdding(true)
    try {
      await createAlias(alias, canonical)
      setAliasInput('')
      setCanonicalInput('')
      const aliasesRes = await getAliases()
      setAliases(aliasesRes.items)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add alias.')
    } finally {
      setAdding(false)
    }
  }

  async function handleDeleteAlias(id: string) {
    setDeletingId(id)
    try {
      await deleteAlias(id)
      setAliases(prev => prev.filter(a => a.id !== id))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete alias.')
    } finally {
      setDeletingId(null)
    }
  }

  if (checkingAccess || !session || session.role !== 'Admin') {
    return null
  }

  return (
    <main className={styles.container}>
      <h1 className={styles.pageTitle}>Intelligence Settings</h1>
      <p className={styles.pageSubtitle}>
        Review and manage what this deployment has learned from your household&apos;s confirmed expenses.
        All processing happens locally — nothing here leaves this server.
      </p>

      {error && <p role="alert" className={styles.error}>{error}</p>}

      {loading ? (
        <p className={styles.loadingText}>Loading…</p>
      ) : (
        <>
          <div className={styles.summaryGrid}>
            <div className={styles.summaryCard}>
              <span className={styles.summaryValue}>{summary?.merchantMappings ?? 0}</span>
              <span className={styles.summaryLabel}>Merchant Mappings</span>
            </div>
            <div className={styles.summaryCard}>
              <span className={styles.summaryValue}>{summary?.fieldTemplates ?? 0}</span>
              <span className={styles.summaryLabel}>Field Templates</span>
            </div>
            <div className={styles.summaryCard}>
              <span className={styles.summaryValue}>{summary?.recurringExpenses ?? 0}</span>
              <span className={styles.summaryLabel}>Recurring Patterns</span>
            </div>
            <div className={styles.summaryCard}>
              <span className={styles.summaryValue}>{summary?.aliases ?? 0}</span>
              <span className={styles.summaryLabel}>Merchant Aliases</span>
            </div>
          </div>

          <section className={styles.section}>
            <h2 className={styles.sectionTitle}>Merchant Aliases</h2>
            <p className={styles.sectionHint}>
              Group merchant name variants (e.g. &ldquo;Woolworths 42&rdquo; and &ldquo;Woolworths 18&rdquo;) under one canonical name
              so categorization, templates, and tag history stay consistent.
            </p>

            {aliases.length > 0 && (
              <table className={styles.table}>
                <thead>
                  <tr>
                    <th className={styles.th}>Alias</th>
                    <th className={styles.th}>Canonical</th>
                    <th className={styles.th}></th>
                  </tr>
                </thead>
                <tbody>
                  {aliases.map(a => (
                    <tr key={a.id}>
                      <td className={styles.td}>{a.aliasNormalized}</td>
                      <td className={styles.td}>{a.canonicalNormalized}</td>
                      <td className={styles.tdActions}>
                        <button
                          type="button"
                          className={styles.deleteButton}
                          onClick={() => void handleDeleteAlias(a.id)}
                          disabled={deletingId === a.id}
                        >
                          {deletingId === a.id ? 'Removing…' : 'Delete'}
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}

            <form onSubmit={handleAddAlias} className={styles.addForm} noValidate>
              <input
                type="text"
                className={styles.input}
                placeholder="Alias (e.g. Woolworths 42)"
                value={aliasInput}
                onChange={e => setAliasInput(e.target.value)}
                maxLength={200}
              />
              <input
                type="text"
                className={styles.input}
                placeholder="Canonical (e.g. Woolworths)"
                value={canonicalInput}
                onChange={e => setCanonicalInput(e.target.value)}
                maxLength={200}
              />
              <button type="submit" className={styles.addButton} disabled={adding}>
                {adding ? 'Adding…' : 'Add alias'}
              </button>
            </form>
          </section>
        </>
      )}
    </main>
  )
}
