import { apiClient } from './client'
import type { LoyaltyBalance } from './types'

/** GET /api/loyalty/balance — logged-in customer's points balance + transaction history (ADR-0039). */
export function getLoyaltyBalance(): Promise<LoyaltyBalance> {
  return apiClient.get<LoyaltyBalance>('/loyalty/balance')
}
