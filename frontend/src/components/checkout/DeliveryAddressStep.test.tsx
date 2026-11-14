import { describe, it, expect, vi, afterEach } from 'vitest'
import { render, screen, fireEvent, cleanup } from '@testing-library/react'
import { AuthContext } from '../../auth/AuthContext'
import { DeliveryAddressStep } from './DeliveryAddressStep'

vi.mock('../../api/ordersApi', () => ({ checkDelivery: vi.fn() }))
vi.mock('../../api/customersApi', () => ({ getMyAddresses: vi.fn().mockResolvedValue([]) }))

afterEach(() => {
  cleanup()
})

describe('DeliveryAddressStep', () => {
  it('links each address field error to its input via aria-describedby', () => {
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
        <DeliveryAddressStep
          address={null}
          deliveryCheck={null}
          onChecked={vi.fn()}
          onSwitchToPickup={vi.fn()}
          onNext={vi.fn()}
          onBack={vi.fn()}
        />
      </AuthContext.Provider>,
    )

    expect(screen.getByLabelText('Ulica').hasAttribute('aria-describedby')).toBe(false)
    fireEvent.click(screen.getByText('Sprawdź dostępność'))

    for (const label of [/^Ulica/, /^Numer budynku/, /^Miasto/, /^Kod pocztowy/]) {
      const input = screen.getByLabelText(label)
      const errorId = input.getAttribute('aria-describedby')
      expect(errorId).toBeTruthy()
      expect(document.getElementById(errorId!)?.textContent).toBeTruthy()
    }
    expect(screen.getByLabelText('Numer lokalu (opcjonalnie)').hasAttribute('aria-describedby')).toBe(false)
  })
})
