import { useEffect, useState } from 'react'
import { getSalesReport } from '../api/reportsApi'
import type { SalesReport } from '../api/types'

function toDateInputValue(date: Date): string {
  return date.toISOString().slice(0, 10)
}

function startOfCurrentMonth(): Date {
  const now = new Date()
  return new Date(now.getFullYear(), now.getMonth(), 1)
}

interface UseSalesReportResult {
  report: SalesReport | null
  loading: boolean
  error: string | null
  /** Date filters as `yyyy-MM-dd`, suitable for an `<input type="date">`. */
  fromDate: string
  toDate: string
  topItems: number
  setFromDate: (value: string) => void
  setToDate: (value: string) => void
  setTopItems: (value: number) => void
}

/**
 * Manages the admin sales report filters (date range, top items count) and refetches
 * GET /api/reports/sales whenever they change.
 */
export function useSalesReport(): UseSalesReportResult {
  const [fromDate, setFromDate] = useState(() => toDateInputValue(startOfCurrentMonth()))
  const [toDate, setToDate] = useState(() => toDateInputValue(new Date()))
  const [topItems, setTopItems] = useState(5)
  const [report, setReport] = useState<SalesReport | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    setLoading(true)
    setError(null)

    const from = new Date(`${fromDate}T00:00:00`).toISOString()
    const to = new Date(`${toDate}T23:59:59`).toISOString()

    getSalesReport({ from, to, topItems })
      .then((result) => {
        if (!cancelled) {
          setReport(result)
        }
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Failed to load sales report')
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
  }, [fromDate, toDate, topItems])

  return { report, loading, error, fromDate, toDate, topItems, setFromDate, setToDate, setTopItems }
}
