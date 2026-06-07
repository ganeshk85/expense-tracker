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
