import type { Promotion } from '../../api/types'

/**
 * Effective state, not just the isActive flag -- mirrors the time-window and usage-limit
 * checks in Promotion.IsQualifiedFor (Domain), so an expired or used-up promotion no longer
 * reads "active" in the table just because nobody deactivated it.
 */
export function promotionStatus(promotion: Promotion, now: Date = new Date()): string {
  if (!promotion.isActive) {
    return 'Nieaktywna'
  }
  if (now < new Date(promotion.validFrom)) {
    return 'Zaplanowana'
  }
  if (now > new Date(promotion.validTo)) {
    return 'Wygasła'
  }
  if (promotion.usageLimit !== null && promotion.usageCount >= promotion.usageLimit) {
    return 'Wyczerpana'
  }
  return 'Aktywna'
}
