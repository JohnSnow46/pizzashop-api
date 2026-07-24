import { useSalesReport } from '../hooks/useSalesReport'

/**
 * `/admin/reports` (RequireAuth roles=RestaurantAdmin/SuperAdmin, mirrors AuthRoles.Admin) —
 * sales report: date range + top-items filters, order count, revenue and a table of the
 * best-selling menu items in that range.
 */
export function AdminReportsPage() {
  const { report, loading, error, fromDate, toDate, topItems, setFromDate, setToDate, setTopItems } = useSalesReport()

  return (
    <div className="admin-page">
      <h2>Panel admina — raporty sprzedaży</h2>

      <section className="admin-section">
        <h3>Filtry</h3>
        <div className="admin-filters">
          <label className="checkout-field">
            Od
            <input type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} max={toDate} />
          </label>
          <label className="checkout-field">
            Do
            <input type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} min={fromDate} />
          </label>
          <label className="checkout-field">
            Top pozycji
            <input
              type="number"
              min={1}
              max={50}
              value={topItems}
              onChange={(e) => setTopItems(Number(e.target.value) || 1)}
            />
          </label>
        </div>
      </section>

      {loading && <p>Ładowanie raportu...</p>}
      {error && <p className="empty-state">{error}</p>}

      {!loading && !error && report && (
        <>
          <section className="admin-section">
            <h3>Podsumowanie</h3>
            <p>Liczba zamówień: {report.orderCount}</p>
            <p>
              Przychód: {report.revenue.amount.toFixed(2)} {report.revenue.currency}
            </p>
          </section>

          <section className="admin-section">
            <h3>Najlepiej sprzedające się pozycje menu</h3>

            {report.topMenuItems.length === 0 ? (
              <p className="empty-state">Brak sprzedaży w wybranym okresie.</p>
            ) : (
              <div className="admin-table-wrap">
                <table className="admin-table">
                  <thead>
                    <tr>
                      <th>Pozycja menu</th>
                      <th>Sprzedana ilość</th>
                      <th>Przychód</th>
                    </tr>
                  </thead>
                  <tbody>
                    {report.topMenuItems.map((item) => (
                      <tr key={item.menuItemId}>
                        <td>{item.menuItemName}</td>
                        <td>{item.quantitySold}</td>
                        <td>
                          {item.revenue.amount.toFixed(2)} {item.revenue.currency}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </>
      )}
    </div>
  )
}
