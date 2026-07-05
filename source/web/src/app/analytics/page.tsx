'use client'

import { useEffect, useState } from 'react'
import { getCategoryTrends, getMerchantDetail, getMerchantRankings } from '@/api/analytics'
import { getOcrAccuracy } from '@/api/intelligence'
import { getSession } from '@/api/expenses'
import type {
  CategoryTrendResponse,
  CategoryTrendSeries,
  MerchantDetailResponse,
  MerchantRankItem,
  OcrAccuracyResponse,
  OcrFieldAccuracyEntry,
  SessionResponse,
} from '@/api/types'
import styles from './analytics.module.css'

// ── Helpers ───────────────────────────────────────────────────────────────────

const MONTH_NAMES = [
  'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
  'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
]

function shortMonth(yyyy_mm: string): string {
  const m = yyyy_mm.split('-')[1] ?? '1'
  return MONTH_NAMES[parseInt(m, 10) - 1] ?? m
}

function formatAmount(n: number): string {
  return `$${n.toFixed(2)}`
}

function formatDate(iso: string | null): string {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}

// ── Tab type ──────────────────────────────────────────────────────────────────

type Tab = 'trends' | 'merchants' | 'ocr-accuracy'

// ── OCR Accuracy table (US-INT-04, Owner-only) ────────────────────────────────

type OcrSortField = 'merchant' | 'field' | 'accuracy'

function OcrAccuracyTable({ data }: { data: OcrAccuracyResponse }) {
  const [sortField, setSortField] = useState<OcrSortField>('accuracy')
  const [sortAsc, setSortAsc] = useState(true)

  function toggleSort(field: OcrSortField) {
    if (field === sortField) {
      setSortAsc(a => !a)
    } else {
      setSortField(field)
      setSortAsc(field !== 'accuracy') // accuracy sorts worst-first by default
    }
  }

  const sufficient = data.items.filter(i => !i.insufficientData)
  const insufficient = data.items.filter(i => i.insufficientData)

  const sorted = [...sufficient].sort((a, b) => {
    let cmp = 0
    if (sortField === 'merchant') cmp = a.merchant.localeCompare(b.merchant)
    else if (sortField === 'field') cmp = a.field.localeCompare(b.field)
    else cmp = (a.accuracyRate ?? 0) - (b.accuracyRate ?? 0)
    return sortAsc ? cmp : -cmp
  })

  const combined: OcrFieldAccuracyEntry[] = [...sorted, ...insufficient]

  if (combined.length === 0) {
    return <p className={styles.emptyHint}>No OCR accuracy data yet. Correct some expenses to start tracking.</p>
  }

  function SortBtn({ field, label }: { field: OcrSortField; label: string }) {
    const active = sortField === field
    return (
      <button
        type="button"
        className={`${styles.sortBtn} ${active ? styles.sortBtnActive : ''}`}
        onClick={() => toggleSort(field)}
        aria-sort={active ? (sortAsc ? 'ascending' : 'descending') : 'none'}
      >
        {label}{active ? (sortAsc ? ' ↑' : ' ↓') : ''}
      </button>
    )
  }

  return (
    <div className={styles.ocrTableWrapper}>
      <table className={styles.ocrTable} aria-label="OCR field accuracy">
        <thead>
          <tr>
            <th><SortBtn field="merchant" label="Merchant" /></th>
            <th><SortBtn field="field" label="Field" /></th>
            <th><SortBtn field="accuracy" label="Accuracy" /></th>
            <th>Samples</th>
          </tr>
        </thead>
        <tbody>
          {combined.map(row => (
            <tr key={`${row.merchant}-${row.field}`}>
              <td>{row.merchant || '—'}</td>
              <td>{row.field}</td>
              <td>
                {row.insufficientData
                  ? <span className={styles.ocrInsufficient}>Not enough data yet</span>
                  : (
                    <span className={
                      (row.accuracyRate ?? 1) >= 0.9
                        ? styles.ocrGood
                        : (row.accuracyRate ?? 1) >= 0.7
                          ? styles.ocrMedium
                          : styles.ocrPoor
                    }>
                      {((row.accuracyRate ?? 0) * 100).toFixed(1)}%
                    </span>
                  )
                }
              </td>
              <td>{row.sampleSize}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

// ── Trend chart ───────────────────────────────────────────────────────────────

interface TrendChartProps {
  data: CategoryTrendResponse
  filtered: CategoryTrendSeries[]
}

function TrendChart({ data, filtered }: TrendChartProps) {
  if (filtered.length === 0) {
    return <p className={styles.emptyHint}>No spending data for the selected category.</p>
  }

  const nonZeroMonths = data.months.filter(m =>
    filtered.some(s => s.data.find(d => d.month === m && d.amount > 0))
  )
  if (nonZeroMonths.length < 2) {
    return (
      <p className={styles.emptyHint}>
        Not enough data yet — add more expenses to see trends.
      </p>
    )
  }

  return (
    <div className={styles.chart} role="img" aria-label="Category trend chart">
      <div className={styles.chartHeader}>
        <div className={styles.labelCol} aria-hidden="true" />
        {data.months.map(m => (
          <div key={m} className={styles.monthCol} aria-hidden="true">
            {shortMonth(m)}
          </div>
        ))}
      </div>

      {filtered.map(series => {
        const peak = Math.max(...series.data.map(d => d.amount), 1)
        const total = series.data.reduce((sum, d) => sum + d.amount, 0)
        return (
          <div key={series.category} className={styles.seriesRow}>
            <div className={styles.labelCol}>
              <span className={styles.seriesCategory}>{series.category}</span>
              <span className={styles.seriesTotal}>{formatAmount(total)}</span>
            </div>
            {series.data.map(d => {
              const heightPct = Math.round((d.amount / peak) * 100)
              const tooltip = `${shortMonth(d.month)} ${d.month.split('-')[0]}: ${formatAmount(d.amount)}${d.isSpiked ? ' ▲ spike >20%' : ''}`
              return (
                <div key={d.month} className={styles.monthCol}>
                  <div className={styles.barCell} title={tooltip}>
                    <div
                      className={`${styles.bar} ${d.isSpiked ? styles.barSpike : styles.barNormal}`}
                      style={{ height: `${heightPct}%` }}
                    />
                  </div>
                </div>
              )
            })}
          </div>
        )
      })}
    </div>
  )
}

// ── Merchant detail panel ─────────────────────────────────────────────────────

interface MerchantDetailPanelProps {
  name: string
  dateFrom: string
  dateTo: string
  onClose: () => void
}

function MerchantDetailPanel({ name, dateFrom, dateTo, onClose }: MerchantDetailPanelProps) {
  const [detail, setDetail] = useState<MerchantDetailResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    setLoading(true)
    setError(null)
    getMerchantDetail(name, dateFrom || undefined, dateTo || undefined)
      .then(setDetail)
      .catch(err => setError(err instanceof Error ? err.message : 'Failed to load.'))
      .finally(() => setLoading(false))
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [name, dateFrom, dateTo])

  return (
    <div className={styles.detailPanel} role="region" aria-label={`Expenses at ${name}`}>
      <div className={styles.detailHeader}>
        <div>
          <span className={styles.detailMerchant}>{name}</span>
          {detail && (
            <span className={styles.detailMeta}>
              {formatAmount(detail.totalSpent)} · {detail.visitCount} visit{detail.visitCount !== 1 ? 's' : ''}
            </span>
          )}
        </div>
        <button className={styles.closeBtn} onClick={onClose} aria-label="Close detail panel">✕</button>
      </div>

      {loading && <p className={styles.loadingText}>Loading…</p>}
      {error && <p className={styles.errorText}>{error}</p>}

      {detail && detail.expenses.length === 0 && (
        <p className={styles.emptyHint}>No expenses found for this merchant.</p>
      )}

      {detail && detail.expenses.length > 0 && (
        <table className={styles.detailTable} aria-label={`Expense list for ${name}`}>
          <thead>
            <tr>
              <th>Date</th>
              <th>Amount</th>
              <th>Category</th>
              <th>Notes</th>
            </tr>
          </thead>
          <tbody>
            {detail.expenses.map(e => (
              <tr key={e.id}>
                <td>{formatDate(e.date)}</td>
                <td className={styles.amountCell}>{e.total != null ? formatAmount(e.total) : '—'}</td>
                <td>{e.category ?? '—'}</td>
                <td className={styles.notesCell}>{e.notes ?? '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}

// ── Main page ─────────────────────────────────────────────────────────────────

export default function AnalyticsPage() {
  const [tab, setTab] = useState<Tab>('trends')
  const [session, setSession] = useState<SessionResponse | null>(null)

  // ── Trends state
  const [trendData, setTrendData] = useState<CategoryTrendResponse | null>(null)
  const [trendLoading, setTrendLoading] = useState(true)
  const [trendError, setTrendError] = useState<string | null>(null)
  const [months, setMonths] = useState(6)
  const [selectedCategory, setSelectedCategory] = useState('')

  // ── Merchants state
  const [merchants, setMerchants] = useState<MerchantRankItem[]>([])
  const [merchantLoading, setMerchantLoading] = useState(false)
  const [merchantError, setMerchantError] = useState<string | null>(null)
  const [dateFrom, setDateFrom] = useState('')
  const [dateTo, setDateTo] = useState('')
  const [openMerchant, setOpenMerchant] = useState<string | null>(null)

  // ── OCR accuracy state (Owner only)
  const [ocrData, setOcrData] = useState<OcrAccuracyResponse | null>(null)
  const [ocrLoading, setOcrLoading] = useState(false)
  const [ocrError, setOcrError] = useState<string | null>(null)

  // Load session once on mount
  useEffect(() => {
    getSession().then(setSession).catch(() => { /* non-critical */ })
  }, [])

  // Load trends whenever months or category changes
  useEffect(() => {
    void loadTrends()
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [months, selectedCategory])

  // Load merchants when merchants tab first becomes active
  useEffect(() => {
    if (tab === 'merchants' && merchants.length === 0 && !merchantLoading) {
      void loadMerchants()
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tab])

  // Load OCR accuracy when tab becomes active (Admin only)
  useEffect(() => {
    if (tab === 'ocr-accuracy' && session?.role === 'Admin' && !ocrData && !ocrLoading) {
      setOcrLoading(true)
      setOcrError(null)
      getOcrAccuracy()
        .then(setOcrData)
        .catch(err => setOcrError(err instanceof Error ? err.message : 'Failed to load OCR accuracy.'))
        .finally(() => setOcrLoading(false))
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tab, session])

  async function loadTrends() {
    setTrendLoading(true)
    setTrendError(null)
    try {
      const res = await getCategoryTrends(months, selectedCategory || undefined)
      setTrendData(res)
    } catch (err) {
      setTrendError(err instanceof Error ? err.message : 'Failed to load trends.')
    } finally {
      setTrendLoading(false)
    }
  }

  async function loadMerchants() {
    setMerchantLoading(true)
    setMerchantError(null)
    try {
      const res = await getMerchantRankings(dateFrom || undefined, dateTo || undefined)
      setMerchants(res.merchants)
    } catch (err) {
      setMerchantError(err instanceof Error ? err.message : 'Failed to load merchants.')
    } finally {
      setMerchantLoading(false)
    }
  }

  function handleMerchantFilter(e: React.FormEvent) {
    e.preventDefault()
    setOpenMerchant(null)
    void loadMerchants()
  }

  const visibleSeries = trendData
    ? selectedCategory
      ? trendData.series.filter(s => s.category === selectedCategory)
      : trendData.series
    : []

  const availableCategories = trendData ? trendData.series.map(s => s.category) : []

  return (
    <main className={styles.container}>
      <h1 className={styles.pageTitle}>Analytics</h1>

      {/* Tab bar */}
      <div className={styles.tabBar} role="tablist">
        <button
          role="tab"
          aria-selected={tab === 'trends'}
          className={`${styles.tab} ${tab === 'trends' ? styles.tabActive : ''}`}
          onClick={() => setTab('trends')}
        >
          Category Trends
        </button>
        <button
          role="tab"
          aria-selected={tab === 'merchants'}
          className={`${styles.tab} ${tab === 'merchants' ? styles.tabActive : ''}`}
          onClick={() => setTab('merchants')}
        >
          Merchant Analytics
        </button>
        {session?.role === 'Admin' && (
          <button
            role="tab"
            aria-selected={tab === 'ocr-accuracy'}
            className={`${styles.tab} ${tab === 'ocr-accuracy' ? styles.tabActive : ''}`}
            onClick={() => setTab('ocr-accuracy')}
          >
            OCR Accuracy
          </button>
        )}
      </div>

      {/* ── Trends tab ──────────────────────────────────────────────── */}
      {tab === 'trends' && (
        <section aria-labelledby="trends-heading">
          <h2 id="trends-heading" className={styles.srOnly}>Category Trends</h2>

          <div className={styles.controls}>
            <label className={styles.controlLabel}>
              Period
              <select
                className={styles.select}
                value={months}
                onChange={e => setMonths(Number(e.target.value))}
              >
                <option value={3}>Last 3 months</option>
                <option value={6}>Last 6 months</option>
                <option value={12}>Last 12 months</option>
              </select>
            </label>

            <label className={styles.controlLabel}>
              Category
              <select
                className={styles.select}
                value={selectedCategory}
                onChange={e => setSelectedCategory(e.target.value)}
              >
                <option value="">All categories</option>
                {availableCategories.map(c => (
                  <option key={c} value={c}>{c}</option>
                ))}
              </select>
            </label>
          </div>

          {trendError && <p role="alert" className={styles.error}>{trendError}</p>}

          {trendLoading ? (
            <p className={styles.loadingText}>Loading trends…</p>
          ) : trendData ? (
            <TrendChart data={trendData} filtered={visibleSeries} />
          ) : null}
        </section>
      )}

      {/* ── Merchants tab ────────────────────────────────────────────── */}
      {tab === 'merchants' && (
        <section aria-labelledby="merchants-heading">
          <h2 id="merchants-heading" className={styles.srOnly}>Merchant Analytics</h2>

          <form className={styles.controls} onSubmit={handleMerchantFilter}>
            <label className={styles.controlLabel}>
              From
              <input
                type="date"
                className={styles.dateInput}
                value={dateFrom}
                onChange={e => setDateFrom(e.target.value)}
              />
            </label>
            <label className={styles.controlLabel}>
              To
              <input
                type="date"
                className={styles.dateInput}
                value={dateTo}
                onChange={e => setDateTo(e.target.value)}
              />
            </label>
            <button type="submit" className={styles.filterBtn}>Apply</button>
          </form>

          {merchantError && <p role="alert" className={styles.error}>{merchantError}</p>}

          {merchantLoading ? (
            <p className={styles.loadingText}>Loading merchants…</p>
          ) : merchants.length === 0 ? (
            <p className={styles.emptyHint}>No merchant data found for the selected period.</p>
          ) : (
            <ol className={styles.merchantList} aria-label="Merchants ranked by total spend">
              {merchants.map((m, i) => (
                <li key={m.merchant}>
                  <button
                    className={`${styles.merchantRow} ${openMerchant === m.merchant ? styles.merchantRowActive : ''}`}
                    onClick={() => setOpenMerchant(prev => prev === m.merchant ? null : m.merchant)}
                    aria-expanded={openMerchant === m.merchant}
                  >
                    <span className={styles.rank}>{i + 1}</span>
                    <span className={styles.merchantName}>{m.merchant}</span>
                    <span className={styles.visitCount}>{m.visitCount} visit{m.visitCount !== 1 ? 's' : ''}</span>
                    <span className={styles.totalSpent}>{formatAmount(m.totalSpent)}</span>
                    <span className={styles.chevron} aria-hidden="true">
                      {openMerchant === m.merchant ? '▲' : '▼'}
                    </span>
                  </button>

                  {openMerchant === m.merchant && (
                    <MerchantDetailPanel
                      name={m.merchant}
                      dateFrom={dateFrom}
                      dateTo={dateTo}
                      onClose={() => setOpenMerchant(null)}
                    />
                  )}
                </li>
              ))}
            </ol>
          )}
        </section>
      )}

      {/* ── OCR Accuracy tab (Admin only) ────────────────────────────── */}
      {tab === 'ocr-accuracy' && session?.role === 'Admin' && (
        <section aria-labelledby="ocr-accuracy-heading">
          <h2 id="ocr-accuracy-heading" className={styles.sectionTitle}>OCR Field Accuracy</h2>
          <p className={styles.ocrSectionHint}>
            Tracks per-field correction rates per merchant. Rows with fewer than 5 samples are shown at the bottom.
            Sort by Accuracy ascending to see worst performers first.
          </p>
          {ocrError && <p role="alert" className={styles.error}>{ocrError}</p>}
          {ocrLoading
            ? <p className={styles.loadingText}>Loading OCR accuracy data…</p>
            : ocrData
              ? <OcrAccuracyTable data={ocrData} />
              : null
          }
        </section>
      )}
    </main>
  )
}
