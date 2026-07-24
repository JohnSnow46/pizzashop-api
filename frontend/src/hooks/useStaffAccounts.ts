import { useCallback, useEffect, useState } from 'react'
import { getStaffAccounts } from '../api/staffApi'
import type { StaffAccount } from '../api/types'

interface UseStaffAccountsResult {
  staffAccounts: StaffAccount[]
  loading: boolean
  error: string | null
  /** Re-fetches the staff list without a full remount — used by the admin staff page. */
  reload: () => void
}

/** Loads the admin staff account list (GET /api/auth/staff) once on mount. */
export function useStaffAccounts(): UseStaffAccountsResult {
  const [staffAccounts, setStaffAccounts] = useState<StaffAccount[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [reloadToken, setReloadToken] = useState(0)

  useEffect(() => {
    let cancelled = false

    setLoading(true)
    setError(null)

    getStaffAccounts()
      .then((result) => {
        if (!cancelled) {
          setStaffAccounts(result)
        }
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Failed to load staff accounts')
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

  return { staffAccounts, loading, error, reload }
}
