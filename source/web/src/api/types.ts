export interface LoginResponse {
  userId: string
  username: string
  role: 'Owner' | 'AdultMember' | 'RestrictedMember'
  mfaRequired: boolean
}

export interface MfaLoginPendingResponse {
  mfaRequired: true
}

export interface MfaSetupResponse {
  secret: string
  otpAuthUri: string
}

export interface MfaToggleRequest {
  enabled: boolean
}

export interface ActivateResponse {
  userId: string
  username: string
  role: 'Owner' | 'AdultMember' | 'RestrictedMember'
}

export interface InviteResponse {
  token: string
  expiresAt: string
}

export interface UploadReceiptResponse {
  receiptId: string
  status: 'Uploaded' | 'Processing' | 'Complete' | 'OcrFailed'
  thumbnailUrl: string | null
  uploadedAt: string
}

export interface ReceiptStatusResponse {
  receiptId: string
  status: 'Uploaded' | 'Processing' | 'Complete' | 'OcrFailed'
  ocrRetryCount: number
  thumbnailUrl: string | null
}

export interface ApiError {
  error: string
}

export interface SessionResponse {
  userId: string
  role: 'Owner' | 'AdultMember' | 'RestrictedMember'
}

export interface ExpenseItemResponse {
  id: string
  name: string
  quantity: number
  unitPrice: number
}

export interface ExpenseResponse {
  id: string
  receiptId: string | null
  userId: string
  merchantName: string | null
  merchantAddress: string | null
  date: string | null
  time: string | null
  subtotal: number | null
  taxAmount: number | null
  total: number | null
  category: string | null
  tags: string[]
  notes: string | null
  source: 'OCR' | 'Manual'
  ocrStatus: string
  confidenceJson: string | null
  items: ExpenseItemResponse[]
  createdAt: string
  updatedAt: string
}

export interface ExpenseListResponse {
  items: ExpenseResponse[]
  total: number
  page: number
  pageSize: number
}

export interface CreateExpenseRequest {
  merchantName?: string
  date?: string
  total: number
  category?: string
  tags?: string[]
  notes?: string
}

export interface UpdateExpenseRequest {
  merchantName?: string
  merchantAddress?: string
  date?: string
  time?: string
  subtotal?: number
  taxAmount?: number
  total?: number
  category?: string
  tags?: string[]
  notes?: string
  items?: Array<{ id?: string; name: string; quantity: number; unitPrice: number }>
}

export interface CorrectExpenseRequest {
  merchantName?: string
  date?: string
  total?: number
  subtotal?: number
  taxAmount?: number
  category?: string
  tags?: string[]
  notes?: string
  items?: Array<{ id?: string; name: string; quantity: number; unitPrice: number }>
}
