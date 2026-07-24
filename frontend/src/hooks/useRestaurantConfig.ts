import { useCallback, useEffect, useState } from 'react'
import { getRestaurantConfig } from '../api/restaurantApi'
import type { RestaurantConfig } from '../api/types'

interface UseRestaurantConfigResult {
  config: RestaurantConfig | null
  loading: boolean
  error: string | null
  /** Re-fetches the restaurant configuration without a full remount. */
  reload: () => void
}

/** Loads restaurant configuration (GET /api/restaurant/config) — admin settings UI. */
export function useRestaurantConfig(): UseRestaurantConfigResult {
  const [config, setConfig] = useState<RestaurantConfig | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [reloadToken, setReloadToken] = useState(0)

  useEffect(() => {
    let cancelled = false

    setLoading(true)
    setError(null)

    getRestaurantConfig()
      .then((result) => {
        if (!cancelled) {
          setConfig(result)
        }
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Nie udało się załadować konfiguracji restauracji.')
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

  return { config, loading, error, reload }
}
