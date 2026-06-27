import { apiClient } from './client'
import type {
  CreateExpenseRequest,
  CorrectExpenseRequest,
  ExpenseListResponse,
  ExpenseResponse,
  SessionResponse,
  UpdateExpenseRequest,
} from './types'

export function getSession(): Promise<SessionResponse> {
  return apiClient.get<SessionResponse>('/auth/session')
}

export function getExpenses(params?: {
  page?: number
  pageSize?: number
  allHousehold?: boolean
}): Promise<ExpenseListResponse> {
  const qs = new URLSearchParams()
  if (params?.page) qs.set('page', String(params.page))
  if (params?.pageSize) qs.set('pageSize', String(params.pageSize))
  if (params?.allHousehold) qs.set('allHousehold', 'true')
  const suffix = qs.toString() ? `?${qs}` : ''
  return apiClient.get<ExpenseListResponse>(`/expenses${suffix}`)
}

export function getExpense(id: string): Promise<ExpenseResponse> {
  return apiClient.get<ExpenseResponse>(`/expenses/${id}`)
}

export function createExpense(body: CreateExpenseRequest): Promise<ExpenseResponse> {
  return apiClient.post<ExpenseResponse>('/expenses', body)
}

export function updateExpense(id: string, body: UpdateExpenseRequest): Promise<ExpenseResponse> {
  return apiClient.put<ExpenseResponse>(`/expenses/${id}`, body)
}

export function correctExpense(id: string, body: CorrectExpenseRequest): Promise<ExpenseResponse> {
  return apiClient.patch<ExpenseResponse>(`/expenses/${id}/corrections`, body)
}

export function deleteExpense(id: string): Promise<void> {
  return apiClient.del<void>(`/expenses/${id}`)
}
