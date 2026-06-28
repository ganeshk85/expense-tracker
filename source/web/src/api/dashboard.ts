import { apiClient } from './client'
import type { DashboardSummaryResponse } from './types'

export function getDashboardSummary(
  month: string,
  view?: 'household' | 'personal'
): Promise<DashboardSummaryResponse> {
  const params = new URLSearchParams({ month })
  if (view) params.set('view', view)
  return apiClient.get<DashboardSummaryResponse>(`/dashboard/summary?${params.toString()}`)
}
