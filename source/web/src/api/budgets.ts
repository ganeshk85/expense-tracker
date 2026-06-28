import { apiClient } from './client'
import type {
  BudgetListResponse,
  BudgetResponse,
  CreateBudgetRequest,
  UpdateBudgetRequest,
} from './types'

export function getBudgets(): Promise<BudgetListResponse> {
  return apiClient.get<BudgetListResponse>('/budgets')
}

export function createBudget(body: CreateBudgetRequest): Promise<BudgetResponse> {
  return apiClient.post<BudgetResponse>('/budgets', body)
}

export function updateBudget(id: string, body: UpdateBudgetRequest): Promise<BudgetResponse> {
  return apiClient.put<BudgetResponse>(`/budgets/${id}`, body)
}

export function deleteBudget(id: string): Promise<void> {
  return apiClient.del<void>(`/budgets/${id}`)
}
