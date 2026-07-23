import { useEffect, useState } from 'react'
import { getMenu } from '../api/menuApi'
import type { MenuItem } from '../api/types'

interface UseMenuResult {
  items: MenuItem[]
  loading: boolean
  error: string | null
}

/** Loads the public menu list (GET /api/menu) once on mount. */
export function useMenu(): UseMenuResult {
  const [items, setItems] = useState<MenuItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    setLoading(true)
    setError(null)

    getMenu()
      .then((result) => {
        if (!cancelled) {
          setItems(result)
        }
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Failed to load menu')
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
  }, [])

  return { items, loading, error }
}
