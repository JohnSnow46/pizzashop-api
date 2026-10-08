import { useEffect, useState } from 'react'
import { getMenuItemReviews } from '../api/reviewsApi'
import type { MenuItemReviews } from '../api/types'

interface UseMenuItemReviewsResult {
  data: MenuItemReviews | null
  loading: boolean
  error: string | null
}

/**
 * Loads a menu item's reviews (GET /api/menu-items/{id}/reviews) only once `enabled` is
 * true — the review list is expandable on `MenuItemCard`, so there is no point fetching
 * it for every card on the menu before the user asks to see it.
 */
export function useMenuItemReviews(menuItemId: string, enabled: boolean): UseMenuItemReviewsResult {
  const [data, setData] = useState<MenuItemReviews | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!enabled || data) {
      return
    }

    let cancelled = false
    setLoading(true)
    setError(null)

    getMenuItemReviews(menuItemId)
      .then((result) => {
        if (!cancelled) {
          setData(result)
        }
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Failed to load reviews')
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false)
        }
      })

    return () => {
      cancelled = true
    }
  }, [enabled, menuItemId, data])

  return { data, loading, error }
}
