import { apiClient } from './client'
import type {
  CreatePromotionCommand,
  Promotion,
  PromotionDiscountPreview,
  UpdatePromotionCommand,
  ValidatePromotionCodeRequest,
} from './types'

/**
 * POST /api/promotions/validate — preview-only discount check for a coupon code (ADR-0036).
 * The real discount is (re)computed server-side when the order is created.
 */
export function validatePromotion(request: ValidatePromotionCodeRequest): Promise<PromotionDiscountPreview> {
  return apiClient.post<PromotionDiscountPreview>('/promotions/validate', request)
}

/** GET /api/promotions — all promotions (Admin role, admin promotions UI). */
export function getPromotions(): Promise<Promotion[]> {
  return apiClient.get<Promotion[]>('/promotions')
}

/** POST /api/promotions — creates a new promotion (Admin role). Returns the new promotion's id. */
export function createPromotion(command: CreatePromotionCommand): Promise<string> {
  return apiClient.post<string>('/promotions', command)
}

/**
 * PUT /api/promotions/{id} — narrow update of a promotion (ADR-0019): only isActive, the
 * valid-from/to window, value and usageLimit are mutable. Admin role.
 */
export function updatePromotion(id: string, command: Omit<UpdatePromotionCommand, 'promotionId'>): Promise<void> {
  return apiClient.put<void>(`/promotions/${id}`, { ...command, promotionId: id })
}
