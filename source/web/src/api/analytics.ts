import { apiClient } from './client'
import type { CategoryTrendResponse, MerchantDetailResponse, MerchantRankingsResponse } from './types'

export function getCategoryTrends(
  months?: number,
  category?: string
): Promise<CategoryTrendResponse> {
  const params = new URLSearchParams()
  if (months) params.set('months', String(months))
  if (category) params.set('category', category)
  const qs = params.toString()
  return apiClient.get<CategoryTrendResponse>(`/analytics/category-trends${qs ? `?${qs}` : ''}`)
}

export function getMerchantRankings(
  dateFrom?: string,
  dateTo?: string
): Promise<MerchantRankingsResponse> {
  const params = new URLSearchParams()
  if (dateFrom) params.set('dateFrom', dateFrom)
  if (dateTo) params.set('dateTo', dateTo)
  const qs = params.toString()
  return apiClient.get<MerchantRankingsResponse>(`/analytics/merchants${qs ? `?${qs}` : ''}`)
}

export function getMerchantDetail(
  name: string,
  dateFrom?: string,
  dateTo?: string
): Promise<MerchantDetailResponse> {
  const params = new URLSearchParams()
  if (dateFrom) params.set('dateFrom', dateFrom)
  if (dateTo) params.set('dateTo', dateTo)
  const qs = params.toString()
  return apiClient.get<MerchantDetailResponse>(
    `/analytics/merchants/${encodeURIComponent(name)}${qs ? `?${qs}` : ''}`
  )
}
