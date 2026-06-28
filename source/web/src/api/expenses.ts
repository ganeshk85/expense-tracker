import { apiClient } from './client'
import type {
  AssignSharesRequest,
  AttachmentListResponse,
  CreateExpenseItemRequest,
  CreateExpenseRequest,
  CorrectExpenseRequest,
  ExpenseAttachmentResponse,
  ExpenseItemResponse,
  ExpenseItemsListResponse,
  ExpenseListResponse,
  ExpenseResponse,
  SearchExpensesParams,
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

// ── Item CRUD ─────────────────────────────────────────────────────────────────

export function getExpenseItems(expenseId: string): Promise<ExpenseItemsListResponse> {
  return apiClient.get<ExpenseItemsListResponse>(`/expenses/${expenseId}/items`)
}

export function addExpenseItem(
  expenseId: string,
  body: CreateExpenseItemRequest
): Promise<ExpenseItemResponse> {
  return apiClient.post<ExpenseItemResponse>(`/expenses/${expenseId}/items`, body)
}

export function updateExpenseItem(
  expenseId: string,
  itemId: string,
  body: CreateExpenseItemRequest
): Promise<ExpenseItemResponse> {
  return apiClient.put<ExpenseItemResponse>(`/expenses/${expenseId}/items/${itemId}`, body)
}

export function deleteExpenseItem(expenseId: string, itemId: string): Promise<void> {
  return apiClient.del<void>(`/expenses/${expenseId}/items/${itemId}`)
}

// ── Shared Expenses ───────────────────────────────────────────────────────────

export function assignShares(
  expenseId: string,
  body: AssignSharesRequest
): Promise<ExpenseResponse> {
  return apiClient.post<ExpenseResponse>(`/expenses/${expenseId}/shares`, body)
}

// ── Receipt Attachment ────────────────────────────────────────────────────────

export function attachReceipt(expenseId: string, receiptId: string): Promise<ExpenseResponse> {
  return apiClient.post<ExpenseResponse>(`/expenses/${expenseId}/receipts/${receiptId}`, {})
}

export function detachReceipt(expenseId: string, receiptId: string): Promise<void> {
  return apiClient.del<void>(`/expenses/${expenseId}/receipts/${receiptId}`)
}

// ── File Attachments ──────────────────────────────────────────────────────────

export function getAttachments(expenseId: string): Promise<AttachmentListResponse> {
  return apiClient.get<AttachmentListResponse>(`/expenses/${expenseId}/attachments`)
}

export function uploadAttachment(expenseId: string, file: File): Promise<ExpenseAttachmentResponse> {
  const form = new FormData()
  form.append('file', file)
  return apiClient.postForm<ExpenseAttachmentResponse>(`/expenses/${expenseId}/attachments`, form)
}

export function deleteAttachment(expenseId: string, attachmentId: string): Promise<void> {
  return apiClient.del<void>(`/expenses/${expenseId}/attachments/${attachmentId}`)
}

// ── Search ────────────────────────────────────────────────────────────────────

export function searchExpenses(params: SearchExpensesParams): Promise<ExpenseListResponse> {
  const qs = new URLSearchParams()
  if (params.q) qs.set('q', params.q)
  if (params.category) qs.set('category', params.category)
  if (params.merchant) qs.set('merchant', params.merchant)
  if (params.dateFrom) qs.set('dateFrom', params.dateFrom)
  if (params.dateTo) qs.set('dateTo', params.dateTo)
  if (params.minAmount != null) qs.set('minAmount', String(params.minAmount))
  if (params.maxAmount != null) qs.set('maxAmount', String(params.maxAmount))
  if (params.tags?.length) params.tags.forEach(t => qs.append('tags', t))
  if (params.page) qs.set('page', String(params.page))
  if (params.pageSize) qs.set('pageSize', String(params.pageSize))
  const suffix = qs.toString() ? `?${qs}` : ''
  return apiClient.get<ExpenseListResponse>(`/expenses/search${suffix}`)
}

