import type { Address } from '../api/types'

/** One-line Polish-style address ("Długa 5/3, 00-001 Warszawa"), shared by the account page's
 * address book and the employee order queue. */
export function formatAddress(address: Address): string {
  const apartment = address.apartmentNumber ? `/${address.apartmentNumber}` : ''
  return `${address.street} ${address.buildingNumber}${apartment}, ${address.postalCode} ${address.city}`
}
