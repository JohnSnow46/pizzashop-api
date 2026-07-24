import { apiClient } from './client'
import type { AddCustomerAddressCommand, CustomerAddress } from './types'

/** GET /api/customers/addresses — logged-in customer's own address book. */
export function getMyAddresses(): Promise<CustomerAddress[]> {
  return apiClient.get<CustomerAddress[]>('/customers/addresses')
}

/** POST /api/customers/addresses — adds a new address-book entry for the logged-in customer. */
export function addAddress(command: AddCustomerAddressCommand): Promise<CustomerAddress> {
  return apiClient.post<CustomerAddress>('/customers/addresses', command)
}

/** DELETE /api/customers/addresses/{id} — removes an address-book entry. */
export function removeAddress(id: string): Promise<void> {
  return apiClient.delete<void>(`/customers/addresses/${id}`)
}

/** PATCH /api/customers/addresses/{id}/default — marks an address-book entry as default. */
export function setDefaultAddress(id: string): Promise<void> {
  return apiClient.patch<void>(`/customers/addresses/${id}/default`, {})
}
