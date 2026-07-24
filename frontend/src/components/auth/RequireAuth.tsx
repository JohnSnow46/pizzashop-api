import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import type { UserRole } from '../../auth/types'
import { useAuth } from '../../hooks/useAuth'

interface RequireAuthProps {
  children: ReactNode
  /** When given, the logged-in user's role must be one of these — otherwise redirect to "/". */
  roles?: UserRole[]
}

/** Wraps /account (ADR-0039) and /employee/orders — a guest has no account panel to see. */
export function RequireAuth({ children, roles }: RequireAuthProps) {
  const { isAuthenticated, user } = useAuth()

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  if (roles && (!user || !roles.includes(user.role))) {
    return <Navigate to="/" replace />
  }

  return <>{children}</>
}
