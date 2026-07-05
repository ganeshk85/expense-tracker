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
  imageQuality: 'good' | 'low' | null
}

export interface ApiError {
  error: string
}

export interface SessionResponse {
  userId: string
  role: 'Admin' | 'Contributor' | 'Reader'
}

// ── Expense Items ─────────────────────────────────────────────────────────────

export interface ExpenseItemResponse {
  id: string
  name: string
  quantity: number
  unitPrice: number
}

export interface ExpenseItemsListResponse {
  items: ExpenseItemResponse[]
}

// ── Expense Shares ────────────────────────────────────────────────────────────

export interface ExpenseShareResponse {
  id: string
  userId: string
  amount: number | null
  percentage: number | null
}

export interface ExpenseShareEntryRequest {
  userId: string
  amount?: number
  percentage?: number
}

export interface AssignSharesRequest {
  shares: ExpenseShareEntryRequest[]
}

// ── Receipts ──────────────────────────────────────────────────────────────────

export interface ReceiptSummaryResponse {
  id: string
  thumbnailUrl: string | null
  status: string
}

// ── Expenses ──────────────────────────────────────────────────────────────────

export interface ExpenseAttachmentResponse {
  id: string
  fileName: string
  contentType: string
  fileSizeBytes: number
  downloadUrl: string
  createdAt: string
}

export interface AttachmentListResponse {
  attachments: ExpenseAttachmentResponse[]
}

// ── Intelligence ──────────────────────────────────────────────────────────────

export interface DuplicateWarning {
  existingExpenseId: string
  existingDate: string | null
  confidence: 'high' | 'possible'
}

export interface TagSuggestionsResponse {
  tags: string[]
}

export interface OcrFieldAccuracyEntry {
  merchant: string
  field: string
  accuracyRate: number | null
  sampleSize: number
  insufficientData: boolean
}

export interface OcrAccuracyResponse {
  items: OcrFieldAccuracyEntry[]
}

export interface MerchantCategoryMapEntry {
  merchantNameNormalized: string
  category: string
  confirmedCount: number
  lastConfirmedAt: string
}

export interface MerchantCategoryMapResponse {
  items: MerchantCategoryMapEntry[]
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
  barcode: string | null
  barcodeType: string | null
  items: ExpenseItemResponse[]
  isShared: boolean
  shares: ExpenseShareResponse[]
  receipts: ReceiptSummaryResponse[]
  attachments: ExpenseAttachmentResponse[]
  createdAt: string
  updatedAt: string
  // Intelligence fields — present only when relevant
  duplicateWarning?: DuplicateWarning
  suggestedCategory?: string
  suggestionConfidence?: 'low' | 'high'
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

export interface CreateExpenseItemRequest {
  name: string
  quantity: number
  unitPrice: number
}

// ── Search ────────────────────────────────────────────────────────────────────

export interface SearchExpensesParams {
  q?: string
  category?: string
  merchant?: string
  dateFrom?: string
  dateTo?: string
  minAmount?: number
  maxAmount?: number
  tags?: string[]
  page?: number
  pageSize?: number
}

// ── Budgets ───────────────────────────────────────────────────────────────────

export interface MemberContributionResponse {
  userId: string
  displayName: string
  contributed: number
}

export interface BudgetResponse {
  id: string
  category: string
  monthlyLimit: number
  type: 'category' | 'household'
  spent: number
  progressPercent: number
  memberBreakdown: MemberContributionResponse[] | null
  createdAt: string
  updatedAt: string
}

export interface BudgetListResponse {
  items: BudgetResponse[]
}

export interface CreateBudgetRequest {
  category: string
  monthlyLimit: number
  type?: 'category' | 'household'
}

export interface UpdateBudgetRequest {
  monthlyLimit: number
}

export interface BudgetHistoryResponse {
  id: string
  budgetId: string
  month: string
  limit: number
  spent: number
}

export interface BudgetHistoryListResponse {
  items: BudgetHistoryResponse[]
}

// ── Notifications ─────────────────────────────────────────────────────────────

export interface NotificationResponse {
  id: string
  type: 'budget_threshold' | 'budget_exceeded' | 'budget_deleted'
  message: string
  budgetId: string | null
  createdAt: string
  dismissedAt: string | null
}

export interface NotificationListResponse {
  notifications: NotificationResponse[]
}

// ── Dashboard ─────────────────────────────────────────────────────────────────

export interface CategoryBreakdownItem {
  category: string
  amount: number
  percentage: number
}

export interface TopMerchantItem {
  merchant: string
  totalSpent: number
  visitCount: number
}

export interface DashboardSummaryResponse {
  month: string
  totalSpent: number
  expenseCount: number
  categoryBreakdown: CategoryBreakdownItem[]
  topMerchants: TopMerchantItem[]
}

// ── Analytics ─────────────────────────────────────────────────────────────────

export interface CategoryMonthDataPoint {
  month: string
  amount: number
  isSpiked: boolean
}

export interface CategoryTrendSeries {
  category: string
  data: CategoryMonthDataPoint[]
}

export interface CategoryTrendResponse {
  months: string[]
  series: CategoryTrendSeries[]
}

export interface MerchantRankItem {
  merchant: string
  totalSpent: number
  visitCount: number
}

export interface MerchantRankingsResponse {
  merchants: MerchantRankItem[]
}

export interface MerchantExpenseItem {
  id: string
  date: string | null
  total: number | null
  category: string | null
  notes: string | null
}

export interface MerchantDetailResponse {
  merchant: string
  totalSpent: number
  visitCount: number
  expenses: MerchantExpenseItem[]
}
