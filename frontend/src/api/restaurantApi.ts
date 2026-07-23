import { apiClient } from './client'
import type { RestaurantConfig } from './types'

/** GET /api/restaurant/config — public restaurant configuration (opening hours, delivery area, thresholds). */
export function getRestaurantConfig(): Promise<RestaurantConfig> {
  return apiClient.get<RestaurantConfig>('/restaurant/config')
}
