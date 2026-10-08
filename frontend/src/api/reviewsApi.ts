import { apiClient } from './client'
import type { CreateReviewCommand, MenuItemReviews, Review } from './types'

/** GET /api/menu-items/{id}/reviews — public, anonymous (ADR-0042). */
export function getMenuItemReviews(menuItemId: string): Promise<MenuItemReviews> {
  return apiClient.get<MenuItemReviews>(`/menu-items/${menuItemId}/reviews`)
}

/** GET /api/reviews/mine — the current customer's own reviews (ADR-0042, Customer role). */
export function getMyReviews(): Promise<Review[]> {
  return apiClient.get<Review[]>('/reviews/mine')
}

/** POST /api/reviews — rate a menu item from one of the caller's own completed orders (ADR-0042). */
export function createReview(command: CreateReviewCommand): Promise<string> {
  return apiClient.post<string>('/reviews', command)
}
