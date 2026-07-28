import { describe, it, expect, vi, afterEach } from 'vitest'
import { render, screen, cleanup } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { AuthContext, type AuthContextValue } from '../../auth/AuthContext'
import { RedirectIfAuthenticated } from './RedirectIfAuthenticated'

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
      <MemoryRouter initialEntries={['/login']}>
        <Routes>
          <Route path="/login" element={<RedirectIfAuthenticated>Login</RedirectIfAuthenticated>} />
          <Route path="/" element={<p>Home</p>} />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  )
}

describe('RedirectIfAuthenticated', () => {
  it('renders nothing while auth is still hydrating, even though isAuthenticated is momentarily false', () => {
    renderWithAuth(makeAuthValue({ isLoading: true, isAuthenticated: false }))

    expect(screen.queryByText('Login')).toBeNull()
    expect(screen.queryByText('Home')).toBeNull()
  })

  it('renders /login once hydration finishes and the user is a guest', () => {
    renderWithAuth(makeAuthValue({ isLoading: false, isAuthenticated: false }))

    expect(screen.getByText('Login')).toBeDefined()
  })

  it('redirects to / once hydration finishes and the user is already authenticated', () => {
    renderWithAuth(makeAuthValue({ isLoading: false, isAuthenticated: true }))

    expect(screen.getByText('Home')).toBeDefined()
  })
})
