import type { AuthResult } from './types'

const STORAGE_KEY = 'pizzashop.auth'

export interface StoredAuth {
  token: string
  user: {
    userAccountId: string
    email: string
    role: AuthResult['role']
    customerId: string | null
    /** Only known right after register() — see AuthContext.AuthUser for details. */
    fullName: string | null
  }
}

export function saveAuth(auth: StoredAuth): void {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(auth))
  } catch {
    // localStorage may be unavailable (private mode, quota) — session just won't survive a
    // refresh in that case, same trade-off as cartStorage.
  }
}

export function loadAuth(): StoredAuth | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) {
      return null
    }
    const parsed: unknown = JSON.parse(raw)
    return parsed && typeof parsed === 'object' ? (parsed as StoredAuth) : null
  } catch {
    // Corrupt/unavailable localStorage should never break the app — start signed out.
    return null
  }
}

export function clearAuth(): void {
  try {
    localStorage.removeItem(STORAGE_KEY)
  } catch {
    // Ignore — nothing to clean up if storage is unavailable.
  }
}
