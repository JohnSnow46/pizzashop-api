import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, cleanup } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import type { CustomerAddress, LoyaltyBalance, OrderSummary } from '../api/types'
import { ApiError } from '../api/client'

vi.mock('../api/ordersApi', () => ({
  getMyOrders: vi.fn(),
}))

vi.mock('../api/loyaltyApi', () => ({
  getLoyaltyBalance: vi.fn(),
}))

vi.mock('../api/customersApi', () => ({
  getMyAddresses: vi.fn(),
  addAddress: vi.fn(),
  removeAddress: vi.fn(),
  setDefaultAddress: vi.fn(),
}))

import { getMyOrders } from '../api/ordersApi'
import { getLoyaltyBalance } from '../api/loyaltyApi'
import { addAddress, getMyAddresses, removeAddress, setDefaultAddress } from '../api/customersApi'
import { MyAccountPage } from './MyAccountPage'

const getMyOrdersMock = vi.mocked(getMyOrders)
const getLoyaltyBalanceMock = vi.mocked(getLoyaltyBalance)
const getMyAddressesMock = vi.mocked(getMyAddresses)
const addAddressMock = vi.mocked(addAddress)
const removeAddressMock = vi.mocked(removeAddress)
const setDefaultAddressMock = vi.mocked(setDefaultAddress)

function makeOrder(overrides: Partial<OrderSummary> = {}): OrderSummary {
  return {
    id: 'order-1',
    number: 'ORD-1',
    placedAt: '2026-01-01T00:00:00Z',
    status: 'Completed',
    fulfillmentType: 'Pickup',
    paymentStatus: 'Paid',
    total: { amount: 42, currency: 'PLN' },
    itemsCount: 2,
    ...overrides,
  }
}

function makeLoyalty(overrides: Partial<LoyaltyBalance> = {}): LoyaltyBalance {
  return {
    pointsBalance: 120,
    transactions: [],
    ...overrides,
  }
}

function makeAddress(overrides: Partial<CustomerAddress> = {}): CustomerAddress {
  return {
    id: 'addr-1',
    label: 'Dom',
    isDefault: true,
    address: {
      street: 'Kwiatowa',
      buildingNumber: '5',
      city: 'Warszawa',
      postalCode: '00-001',
      apartmentNumber: null,
      notes: null,
    },
    ...overrides,
  }
}

beforeEach(() => {
  getMyOrdersMock.mockReset()
  getLoyaltyBalanceMock.mockReset()
  getMyAddressesMock.mockReset()
  addAddressMock.mockReset()
  removeAddressMock.mockReset()
  setDefaultAddressMock.mockReset()

  getMyOrdersMock.mockResolvedValue([makeOrder()])
  getLoyaltyBalanceMock.mockResolvedValue(makeLoyalty())
  getMyAddressesMock.mockResolvedValue([makeAddress()])
})

afterEach(() => {
  cleanup()
})

function renderPage() {
  render(
    <MemoryRouter>
      <MyAccountPage />
    </MemoryRouter>,
  )
}

describe('MyAccountPage', () => {
  it('renders order history, loyalty balance and addresses after loading', async () => {
    renderPage()

    expect(await screen.findByText('ORD-1')).toBeDefined()
    expect(screen.getByText('120 pkt')).toBeDefined()
    expect(screen.getByText('Dom')).toBeDefined()
  })

  it('shows empty-state messages when there are no orders, transactions or addresses', async () => {
    getMyOrdersMock.mockResolvedValue([])
    getLoyaltyBalanceMock.mockResolvedValue(makeLoyalty({ pointsBalance: 0, transactions: [] }))
    getMyAddressesMock.mockResolvedValue([])

    renderPage()

    expect(await screen.findByText('Nie masz jeszcze żadnych zamówień.')).toBeDefined()
    expect(screen.getByText('Brak historii punktów.')).toBeDefined()
    expect(screen.getByText('Nie masz jeszcze zapisanych adresów.')).toBeDefined()
  })

  it('shows an error message when the initial fetch fails', async () => {
    getMyOrdersMock.mockRejectedValue(new Error('Nie udało się połączyć z serwerem.'))

    renderPage()

    expect(await screen.findByText('Nie udało się połączyć z serwerem.')).toBeDefined()
  })

  it('validates the new address form and does not call addAddress when fields are empty', async () => {
    renderPage()
    await screen.findByText('Dom')

    fireEvent.click(screen.getByRole('button', { name: 'Dodaj adres' }))

    expect(await screen.findByText('Podaj nazwę adresu.')).toBeDefined()
    expect(screen.getByText('Podaj ulicę.')).toBeDefined()
    expect(addAddressMock).not.toHaveBeenCalled()
  })

  it('adds a new address, shows it in the list and resets the form', async () => {
    const created = makeAddress({ id: 'addr-2', label: 'Praca', isDefault: false })
    addAddressMock.mockResolvedValue(created)

    renderPage()
    await screen.findByText('Dom')

    fireEvent.change(screen.getByLabelText('Nazwa adresu (np. Dom, Praca)'), { target: { value: 'Praca' } })
    fireEvent.change(screen.getByLabelText('Ulica'), { target: { value: 'Robocza' } })
    fireEvent.change(screen.getByLabelText('Numer budynku'), { target: { value: '10' } })
    fireEvent.change(screen.getByLabelText('Miasto'), { target: { value: 'Kraków' } })
    fireEvent.change(screen.getByLabelText('Kod pocztowy'), { target: { value: '30-001' } })

    fireEvent.click(screen.getByRole('button', { name: 'Dodaj adres' }))

    expect(await screen.findByText('Praca')).toBeDefined()
    expect(addAddressMock).toHaveBeenCalledWith({
      label: 'Praca',
      address: {
        street: 'Robocza',
        buildingNumber: '10',
        city: 'Kraków',
        postalCode: '30-001',
        apartmentNumber: null,
        notes: null,
      },
      isDefault: false,
    })
    expect((screen.getByLabelText('Nazwa adresu (np. Dom, Praca)') as HTMLInputElement).value).toBe('')
  })

  it('shows an API error and keeps the entry when removing an address fails', async () => {
    removeAddressMock.mockRejectedValue(new ApiError(409, 'Conflict', { detail: 'Nie można usunąć adresu domyślnego.' }))

    renderPage()
    await screen.findByText('Dom')

    fireEvent.click(screen.getByRole('button', { name: 'Usuń' }))

    expect(await screen.findByText('Nie można usunąć adresu domyślnego.')).toBeDefined()
    expect(screen.getByText('Dom')).toBeDefined()
    expect(setDefaultAddressMock).not.toHaveBeenCalled()
  })
})
