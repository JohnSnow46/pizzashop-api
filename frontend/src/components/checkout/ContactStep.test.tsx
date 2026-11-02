import { describe, it, expect, vi, afterEach } from 'vitest'
import { render, screen, fireEvent, cleanup } from '@testing-library/react'
import { AuthContext } from '../../auth/AuthContext'
import { ContactStep } from './ContactStep'

afterEach(() => {
  cleanup()
})

describe('ContactStep', () => {
  it('marks only the invalid fields as aria-invalid after "Dalej"', () => {
    const onNext = vi.fn()
    render(
      <AuthContext.Provider
        value={{
          token: null,
          user: null,
          isAuthenticated: false,
          isLoading: false,
          login: vi.fn(),
          register: vi.fn(),
          logout: vi.fn(),
        }}
      >
        <ContactStep
          contact={{ fullName: 'Jan Kowalski', phoneNumber: '', email: null }}
          onChange={vi.fn()}
          onNext={onNext}
          onBack={vi.fn()}
        />
      </AuthContext.Provider>,
    )

    expect(screen.getByLabelText('Telefon').getAttribute('aria-invalid')).toBe('false')
    fireEvent.click(screen.getByText('Dalej'))

    expect(onNext).not.toHaveBeenCalled()
    expect(screen.getByLabelText('Imię i nazwisko').getAttribute('aria-invalid')).toBe('false')
    expect(screen.getByLabelText(/^Telefon/).getAttribute('aria-invalid')).toBe('true')
    expect(screen.getByLabelText('E-mail (opcjonalnie)').getAttribute('aria-invalid')).toBe('false')
  })
})
