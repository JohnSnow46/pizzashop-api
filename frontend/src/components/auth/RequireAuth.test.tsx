import { describe, it, expect, vi, afterEach } from 'vitest'
import { render, screen, cleanup } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { AuthContext, type AuthContextValue } from '../../auth/AuthContext'
import { RequireAuth } from './RequireAuth'

afterEach(() => {
  cleanup()
})

function makeAuthValue(overrides: Partial<AuthContextValue> = {}): AuthContextValue {
  return {
    user: null,
    token: null,
    isAuthenticated: false,
    isLoading: false,
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
    ...overrides,
  }
}

function renderWithAuth(authValue: AuthContextValue) {
  return render(
    <AuthContext.Provider value={authValue}>
      <MemoryRouter initialEntries={['/account']}>
        <Routes>
          <Route path="/account" element={<RequireAuth>Chronione</RequireAuth>} />
          <Route path="/login" element={<p>Login</p>} />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  )
}

describe('RequireAuth', () => {
  it('renders nothing while auth is still hydrating, even though isAuthenticated is momentarily false', () => {
    renderWithAuth(makeAuthValue({ isLoading: true, isAuthenticated: false }))

    expect(screen.queryByText('Chronione')).toBeNull()
    expect(screen.queryByText('Login')).toBeNull()
  })

  it('redirects to /login once hydration finishes and the user is not authenticated', () => {
    renderWithAuth(makeAuthValue({ isLoading: false, isAuthenticated: false }))

    expect(screen.getByText('Login')).toBeDefined()
  })

  it('renders the protected content once hydration finishes and the user is authenticated', () => {
    renderWithAuth(
      makeAuthValue({
        isLoading: false,
        isAuthenticated: true,
        user: { userAccountId: 'u1', email: 'a@b.pl', role: 'Customer', customerId: 'c1', fullName: null },
      }),
    )

    expect(screen.getByText('Chronione')).toBeDefined()
  })
})
