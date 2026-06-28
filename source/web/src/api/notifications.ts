import { apiClient } from './client'
import type { NotificationListResponse } from './types'

export function getNotifications(): Promise<NotificationListResponse> {
  return apiClient.get<NotificationListResponse>('/notifications')
}

export function dismissNotification(id: string): Promise<void> {
  return apiClient.post<void>(`/notifications/${id}/dismiss`, {})
}
