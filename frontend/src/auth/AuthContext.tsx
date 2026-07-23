import { createContext, useEffect, useState, type ReactNode } from 'react'
import { setAuthToken } from '../api/client'
import * as authApi from './authApi'
import { clearAuth, loadAuth, saveAuth, type StoredAuth } from './authStorage'
import type { UserRole } from './types'

export interface AuthUser {
  userAccountId: string
  email: string
  role: UserRole
  customerId: string | null
  /**
   * Only known right after register() (the value the user just typed in) — AuthResultDto
   * carries neither fullName nor email, so a returning user who logs in later has this as
   * null (no /me profile call in this iteration, ADR-0037).
   */
  fullName: string | null
}

export interface AuthContextValue {
  user: AuthUser | null
  token: string | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string, fullName: string, phoneNumber: string | null) => Promise<void>
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)

/** Mirrors backend UserAccount.NormalizeEmail (Trim().ToLowerInvariant()) so the displayed
 * email matches what's actually stored on the account regardless of casing typed at login. */
function normalizeEmail(email: string): string {
  return email.trim().toLowerCase()
}

/**
 * Auth session state (ADR-0037). Token/user persisted to localStorage (pizzashop.auth) so a
 * refresh keeps the user signed in; the token is treated optimistically until the Api returns
 * a 401 — there's no proactive /me validation call on mount.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [stored, setStored] = useState<StoredAuth | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    const existing = loadAuth()
    setStored(existing)
    setAuthToken(existing?.token ?? null)
    setIsLoading(false)
  }, [])

  async function login(email: string, password: string): Promise<void> {
    const result = await authApi.login({ email, password })
    const next: StoredAuth = {
      token: result.token,
      user: {
        userAccountId: result.userAccountId,
        email: normalizeEmail(email),
        role: result.role,
        customerId: result.customerId,
        fullName: null,
      },
    }
    saveAuth(next)
    setAuthToken(next.token)
    setStored(next)
  }

  async function register(
    email: string,
    password: string,
    fullName: string,
    phoneNumber: string | null,
  ): Promise<void> {
    const result = await authApi.register({ email, password, fullName, phoneNumber })
    const next: StoredAuth = {
      token: result.token,
      user: {
        userAccountId: result.userAccountId,
        email: normalizeEmail(email),
        role: result.role,
        customerId: result.customerId,
        fullName,
      },
    }
    saveAuth(next)
    setAuthToken(next.token)
    setStored(next)
  }

  function logout(): void {
    clearAuth()
    setAuthToken(null)
    setStored(null)
  }

  const value: AuthContextValue = {
    user: stored?.user ?? null,
    token: stored?.token ?? null,
    isAuthenticated: stored !== null,
    isLoading,
    login,
    register,
    logout,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
