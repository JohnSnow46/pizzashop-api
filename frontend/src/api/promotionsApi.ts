import { apiClient } from './client'
import type { PromotionDiscountPreview, ValidatePromotionCodeRequest } from './types'

/**
 * POST /api/promotions/validate — preview-only discount check for a coupon code (ADR-0036).
 * The real discount is (re)computed server-side when the order is created.
 */
export function validatePromotion(request: ValidatePromotionCodeRequest): Promise<PromotionDiscountPreview> {
  return apiClient.post<PromotionDiscountPreview>('/promotions/validate', request)
}
