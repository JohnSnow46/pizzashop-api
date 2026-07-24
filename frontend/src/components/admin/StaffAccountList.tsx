import type { StaffAccount } from '../../api/types'

interface StaffAccountListProps {
  staffAccounts: StaffAccount[]
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('pl-PL')
}

/** Read-only table of staff accounts (list only — no edit/deactivate action, admin staff UI). */
export function StaffAccountList({ staffAccounts }: StaffAccountListProps) {
  if (staffAccounts.length === 0) {
    return <p className="empty-state">Brak kont pracowników.</p>
  }

  return (
    <div className="admin-table-wrap">
      <table className="admin-table">
        <thead>
          <tr>
            <th>Email</th>
            <th>Rola</th>
            <th>Aktywne</th>
            <th>Utworzono</th>
          </tr>
        </thead>
        <tbody>
          {staffAccounts.map((account) => (
            <tr key={account.id}>
              <td>{account.email}</td>
              <td>{account.role}</td>
              <td>{account.isActive ? 'Tak' : 'Nie'}</td>
              <td>{formatDate(account.createdAt)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
