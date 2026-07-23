import { useContext } from 'react'
import { AuthContext, type AuthContextValue } from '../auth/AuthContext'

/** Access to the auth session state/actions from AuthProvider (must be rendered above the caller). */
export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
