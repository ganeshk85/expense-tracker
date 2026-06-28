import { apiClient } from './client'
import type { ReceiptStatusResponse, UploadReceiptResponse } from './types'

export function uploadReceipt(file: File): Promise<UploadReceiptResponse> {
  const form = new FormData()
  form.append('file', file)
  return apiClient.postForm<UploadReceiptResponse>('/receipts/upload', form)
}

export function getReceiptStatus(receiptId: string): Promise<ReceiptStatusResponse> {
  return apiClient.get<ReceiptStatusResponse>(`/receipts/${receiptId}/status`)
}
