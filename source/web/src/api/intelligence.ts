import { apiClient } from './client'
import type {
  IntelligenceSummaryResponse,
  MerchantAliasEntry,
  MerchantAliasesResponse,
  MerchantCategoryMapResponse,
  MerchantFieldTemplatesResponse,
  OcrAccuracyResponse,
  RecurringExpensesResponse,
  TagSuggestionsResponse,
} from './types'

export function getTagSuggestions(merchant: string): Promise<TagSuggestionsResponse> {
  const qs = new URLSearchParams({ merchant })
  return apiClient.get<TagSuggestionsResponse>(`/intelligence/tag-suggestions?${qs}`)
}

export function getMerchantCategoryMap(): Promise<MerchantCategoryMapResponse> {
  return apiClient.get<MerchantCategoryMapResponse>('/intelligence/merchant-map')
}

export function getOcrAccuracy(): Promise<OcrAccuracyResponse> {
  return apiClient.get<OcrAccuracyResponse>('/intelligence/ocr-accuracy')
}

// ── US-INT-05: Merchant field templates ──────────────────────────────────────

export function getMerchantTemplates(): Promise<MerchantFieldTemplatesResponse> {
  return apiClient.get<MerchantFieldTemplatesResponse>('/intelligence/merchant-templates')
}

export function deleteMerchantTemplate(merchantNormalized: string): Promise<void> {
  return apiClient.del<void>(`/intelligence/merchant-templates/${encodeURIComponent(merchantNormalized)}`)
}

// ── US-INT-06: Recurring expenses ─────────────────────────────────────────────

export function getRecurring(): Promise<RecurringExpensesResponse> {
  return apiClient.get<RecurringExpensesResponse>('/intelligence/recurring')
}

export function snoozeRecurring(id: string, days = 30): Promise<void> {
  return apiClient.post<void>(`/intelligence/recurring/${id}/snooze?days=${days}`, undefined)
}

// ── US-INT-07: Merchant aliases ───────────────────────────────────────────────

export function getAliases(): Promise<MerchantAliasesResponse> {
  return apiClient.get<MerchantAliasesResponse>('/intelligence/merchant-aliases')
}

export function createAlias(alias: string, canonical: string): Promise<MerchantAliasEntry> {
  return apiClient.post<MerchantAliasEntry>('/intelligence/merchant-aliases', { alias, canonical })
}

export function deleteAlias(id: string): Promise<void> {
  return apiClient.del<void>(`/intelligence/merchant-aliases/${id}`)
}

// ── US-INT-08: Intelligence settings summary ──────────────────────────────────

export function getIntelligenceSummary(): Promise<IntelligenceSummaryResponse> {
  return apiClient.get<IntelligenceSummaryResponse>('/intelligence/summary')
}
