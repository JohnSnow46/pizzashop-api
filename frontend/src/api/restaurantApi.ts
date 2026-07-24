import { apiClient } from './client'
import type {
  RestaurantConfig,
  UpdateDeliveryAreaCommand,
  UpdateOpeningHoursCommand,
  UpdateOrderingThresholdsCommand,
} from './types'

/** GET /api/restaurant/config — public restaurant configuration (opening hours, delivery area, thresholds). */
export function getRestaurantConfig(): Promise<RestaurantConfig> {
  return apiClient.get<RestaurantConfig>('/restaurant/config')
}

/** PUT /api/restaurant/opening-hours — Admin only. */
export function updateOpeningHours(command: UpdateOpeningHoursCommand): Promise<void> {
  return apiClient.put<void>('/restaurant/opening-hours', command)
}

/** PUT /api/restaurant/delivery-area — Admin only. */
export function updateDeliveryArea(command: UpdateDeliveryAreaCommand): Promise<void> {
  return apiClient.put<void>('/restaurant/delivery-area', command)
}

/** PUT /api/restaurant/ordering-thresholds — Admin only. */
export function updateOrderingThresholds(command: UpdateOrderingThresholdsCommand): Promise<void> {
  return apiClient.put<void>('/restaurant/ordering-thresholds', command)
}
