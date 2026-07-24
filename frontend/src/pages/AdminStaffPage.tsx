import { useState } from 'react'
import { StaffAccountForm } from '../components/admin/StaffAccountForm'
import { StaffAccountList } from '../components/admin/StaffAccountList'
import { useStaffAccounts } from '../hooks/useStaffAccounts'

/**
 * `/admin/staff` (RequireAuth roles=RestaurantAdmin/SuperAdmin, mirrors AuthRoles.Admin) —
 * staff account management: list existing accounts + create new ones via
 * RegisterStaffAccountCommand (list + create only, no edit/deactivate in this iteration).
 */
export function AdminStaffPage() {
  const { staffAccounts, loading, error, reload } = useStaffAccounts()
  const [formOpen, setFormOpen] = useState(false)

  function handleCreated() {
    setFormOpen(false)
    reload()
  }

  return (
    <div className="admin-page">
      <h2>Panel admina — pracownicy</h2>

      <section className="admin-section">
        <h3>Konta pracowników</h3>

        {loading && <p>Ładowanie kont...</p>}
        {error && <p className="empty-state">{error}</p>}

        {!loading && !error && <StaffAccountList staffAccounts={staffAccounts} />}

        {!formOpen && (
          <button type="button" onClick={() => setFormOpen(true)}>
            Dodaj nowe konto
          </button>
        )}

        {formOpen && <StaffAccountForm onCreated={handleCreated} onCancel={() => setFormOpen(false)} />}
      </section>
    </div>
  )
}
