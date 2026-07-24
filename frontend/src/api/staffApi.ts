import { apiClient } from './client'
import type { AuthResult } from '../auth/types'
import type { CreateStaffAccountCommand, StaffAccount } from './types'

/** GET /api/auth/staff — all staff accounts, excludes Customer (Admin role, admin staff UI). */
export function getStaffAccounts(): Promise<StaffAccount[]> {
  return apiClient.get<StaffAccount[]>('/auth/staff')
}

/**
 * POST /api/auth/staff — creates a staff account (Admin role). Returns the same
 * AuthResultDto shape as login/register, but the admin staff UI only needs it to confirm
 * success — it does not log the caller in as the new account.
 */
export function createStaffAccount(command: CreateStaffAccountCommand): Promise<AuthResult> {
  return apiClient.post<AuthResult>('/auth/staff', command)
}
