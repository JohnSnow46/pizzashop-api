import { describe, it, expect, vi, afterEach } from 'vitest'
import { render, screen, fireEvent, cleanup } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { AuthContext } from '../auth/AuthContext'
import { ApiError } from '../api/client'
import { LoginPage } from './LoginPage'

function renderLoginPage(login = vi.fn().mockResolvedValue(undefined)) {
  render(
    <AuthContext.Provider
      value={{
        token: null,
        user: null,
        isAuthenticated: false,
        isLoading: false,
        login,
        register: vi.fn(),
        logout: vi.fn(),
      }}
    >
      <MemoryRouter initialEntries={['/login']}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/" element={<p>Strona główna</p>} />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  )
  return { login }
}

afterEach(() => {
  cleanup()
})

describe('LoginPage', () => {
  it('logs in with valid credentials and redirects to home', async () => {
    const { login } = renderLoginPage()

    fireEvent.change(screen.getByLabelText('E-mail'), { target: { value: 'user@test.pl' } })
    fireEvent.change(screen.getByLabelText('Hasło'), { target: { value: 'password123' } })
    fireEvent.click(screen.getByRole('button', { name: 'Zaloguj się' }))

    await screen.findByText('Strona główna')
    expect(login).toHaveBeenCalledWith('user@test.pl', 'password123')
  })

  it('shows validation errors and does not call login when fields are empty', () => {
    const { login } = renderLoginPage()

    fireEvent.click(screen.getByRole('button', { name: 'Zaloguj się' }))

    expect(screen.getByText('Podaj adres e-mail.')).toBeDefined()
    expect(screen.getByText('Podaj hasło.')).toBeDefined()
    expect(login).not.toHaveBeenCalled()
  })

  it('shows an invalid-credentials banner on a 401 response', async () => {
    renderLoginPage(vi.fn().mockRejectedValue(new ApiError(401, 'Unauthorized')))

    fireEvent.change(screen.getByLabelText('E-mail'), { target: { value: 'user@test.pl' } })
    fireEvent.change(screen.getByLabelText('Hasło'), { target: { value: 'password123' } })
    fireEvent.click(screen.getByRole('button', { name: 'Zaloguj się' }))

    await screen.findByText('Nieprawidłowy e-mail lub hasło.')
  })

  it('shows a ProblemDetails message for a non-401 ApiError', async () => {
    renderLoginPage(vi.fn().mockRejectedValue(new ApiError(500, 'Server error', { title: 'Błąd serwera', detail: 'Spróbuj później.' })))

    fireEvent.change(screen.getByLabelText('E-mail'), { target: { value: 'user@test.pl' } })
    fireEvent.change(screen.getByLabelText('Hasło'), { target: { value: 'password123' } })
    fireEvent.click(screen.getByRole('button', { name: 'Zaloguj się' }))

    await screen.findByText('Spróbuj później.')
  })
})
