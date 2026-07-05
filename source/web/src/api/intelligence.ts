import { apiClient } from './client'
import type {
  MerchantCategoryMapResponse,
  OcrAccuracyResponse,
  TagSuggestionsResponse,
} from './types'

export function getTagSuggestions(merchant: string): Promise<TagSuggestionsResponse> {
  const qs = new URLSearchParams({ merchant })
  return apiClient.get<TagSuggestionsResponse>(`/intelligence/tag-suggestions?${qs}`)
}

export function getMerchantCategoryMap(): Promise<MerchantCategoryMapResponse> {
  return apiClient.get<MerchantCategoryMapResponse>('/intelligence/merchant-map')
}

export function getOcrAccuracy(): Promise<OcrAccuracyResponse> {
  return apiClient.get<OcrAccuracyResponse>('/intelligence/ocr-accuracy')
}
