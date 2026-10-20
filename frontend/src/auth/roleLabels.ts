import type { UserRole } from './types'

/** Polish label per UserRole — shared by the staff account form and the staff list. */
export const ROLE_LABELS: Record<UserRole, string> = {
  Customer: 'Klient',
  Employee: 'Pracownik',
  RestaurantAdmin: 'Administrator restauracji',
  SuperAdmin: 'Super administrator',
}
