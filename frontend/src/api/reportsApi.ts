import { apiClient } from './client'
import type { SalesReport } from './types'

export interface GetSalesReportParams {
  /** ISO 8601 datetime, inclusive lower bound. */
  from: string
  /** ISO 8601 datetime, inclusive upper bound. */
  to: string
  topItems?: number
}

/** GET /api/reports/sales — order count, revenue and top-selling menu items for a date range (Admin role). */
export function getSalesReport({ from, to, topItems = 5 }: GetSalesReportParams): Promise<SalesReport> {
  const query = new URLSearchParams({ from, to, topItems: String(topItems) })
  return apiClient.get<SalesReport>(`/reports/sales?${query.toString()}`)
}
