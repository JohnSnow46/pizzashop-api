import { describe, it, expect, vi, afterEach } from 'vitest'
import { render, screen, fireEvent, cleanup } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { AuthContext } from '../auth/AuthContext'
import { RegisterPage } from './RegisterPage'

afterEach(() => {
  cleanup()
})

describe('RegisterPage', () => {
  it('links each field error to its input via aria-describedby', () => {
    const register = vi.fn()
    render(
      <AuthContext.Provider
        value={{
          token: null,
          user: null,
          isAuthenticated: false,
          isLoading: false,
          login: vi.fn(),
          register,
          logout: vi.fn(),
        }}
      >
        <MemoryRouter>
          <RegisterPage />
        </MemoryRouter>
      </AuthContext.Provider>,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Załóż konto' }))

    expect(register).not.toHaveBeenCalled()
    for (const label of [/^Imię i nazwisko/, /^E-mail/, /^Hasło/]) {
      const errorId = screen.getByLabelText(label).getAttribute('aria-describedby')
      expect(errorId).toBeTruthy()
      expect(document.getElementById(errorId!)?.textContent).toBeTruthy()
    }
    expect(screen.getByLabelText('Telefon (opcjonalnie)').hasAttribute('aria-describedby')).toBe(false)
  })
})
