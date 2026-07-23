import { apiClient } from '../api/client'
import type { AuthResult, LoginRequest, RegisterRequest } from './types'

/** POST /api/auth/login (ADR-0037). */
export function login(req: LoginRequest): Promise<AuthResult> {
  return apiClient.post<AuthResult>('/auth/login', req)
}

/** POST /api/auth/register — creates Customer + LoyaltyAccount atomically, auto-login (ADR-0037). */
export function register(req: RegisterRequest): Promise<AuthResult> {
  return apiClient.post<AuthResult>('/auth/register', req)
}
