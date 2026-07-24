import { useCallback, useEffect, useState } from 'react'
import { getPromotions } from '../api/promotionsApi'
import type { Promotion } from '../api/types'

interface UsePromotionsResult {
  promotions: Promotion[]
  loading: boolean
  error: string | null
  /** Re-fetches the promotion list without a full remount — used by the admin promotions page. */
  reload: () => void
}

/** Loads the admin promotion list (GET /api/promotions) once on mount. */
export function usePromotions(): UsePromotionsResult {
  const [promotions, setPromotions] = useState<Promotion[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [reloadToken, setReloadToken] = useState(0)

  useEffect(() => {
    let cancelled = false

    setLoading(true)
    setError(null)

    getPromotions()
      .then((result) => {
        if (!cancelled) {
          setPromotions(result)
        }
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Failed to load promotions')
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
  }, [reloadToken])

  const reload = useCallback(() => setReloadToken((token) => token + 1), [])

  return { promotions, loading, error, reload }
}
