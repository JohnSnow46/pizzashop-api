import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, cleanup } from '@testing-library/react'
import type { Money, Order } from '../../api/types'

vi.mock('../../api/ordersApi', () => ({
  acceptOrder: vi.fn(),
  cancelOrder: vi.fn(),
  completeOrder: vi.fn(),
  markReady: vi.fn(),
  rejectOrder: vi.fn(),
  startDelivery: vi.fn(),
  startPreparation: vi.fn(),
}))

const useOrderTrackingMock = vi.fn()
vi.mock('../../hooks/useOrderTracking', () => ({
  useOrderTracking: () => useOrderTrackingMock(),
}))

import { EmployeeOrderRow } from './EmployeeOrderRow'

function money(amount: number): Money {
  return { amount, currency: 'PLN' }
}

function makeOrder(overrides: Partial<Order> = {}): Order {
  return {
    id: 'order-1',
    number: '1001',
    customerId: null,
    contact: { fullName: 'Jan Kowalski', phoneNumber: '123456789', email: null },
    fulfillmentType: 'Pickup',
    deliveryAddress: null,
    placedAt: '2026-10-21T10:00:00Z',
    requestedFulfillmentTime: null,
    estimatedReadyAt: null,
    status: 'Accepted',
    paymentMethod: 'OnPickup',
    paymentStatus: 'Pending',
    subtotal: money(40),
    discountAmount: money(0),
    deliveryFee: money(0),
    total: money(40),
    items: [],
    ...overrides,
  }
}

function renderWith(order: Order) {
  useOrderTrackingMock.mockReturnValue({ order, isLoading: false, error: null })
  render(<EmployeeOrderRow orderId={order.id} onLeftQueue={vi.fn()} />)
}

beforeEach(() => {
  useOrderTrackingMock.mockReset()
})

afterEach(() => {
  cleanup()
})

describe('EmployeeOrderRow', () => {
  it('shows the delivery address and its notes for a delivery order', () => {
    renderWith(
      makeOrder({
        fulfillmentType: 'Delivery',
        deliveryAddress: {
          street: 'Długa',
          buildingNumber: '5',
          apartmentNumber: '3',
          city: 'Warszawa',
          postalCode: '00-001',
          notes: 'Domofon nie działa',
        },
      }),
    )

    expect(screen.getByText('Adres: Długa 5/3, 00-001 Warszawa · Domofon nie działa')).not.toBeNull()
  })

  it('shows no address line for a pickup order', () => {
    renderWith(makeOrder())

    expect(screen.queryByText(/Adres:/)).toBeNull()
  })
})
