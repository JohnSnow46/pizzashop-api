import { useCallback, useEffect, useState } from 'react'
import { getIngredients } from '../api/ingredientsApi'
import type { Ingredient } from '../api/types'

interface UseIngredientsResult {
  ingredients: Ingredient[]
  loading: boolean
  error: string | null
  /** Re-fetches the ingredient dictionary without a full remount. */
  reload: () => void
}

/** Loads the ingredient dictionary (GET /api/ingredients, Admin role) — admin catalog UI only. */
export function useIngredients(): UseIngredientsResult {
  const [ingredients, setIngredients] = useState<Ingredient[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [reloadToken, setReloadToken] = useState(0)

  useEffect(() => {
    let cancelled = false

    setLoading(true)
    setError(null)

    getIngredients()
      .then((result) => {
        if (!cancelled) {
          setIngredients(result)
        }
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Nie udało się załadować składników.')
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

  return { ingredients, loading, error, reload }
}
