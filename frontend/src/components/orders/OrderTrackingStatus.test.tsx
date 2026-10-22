import { describe, it, expect, afterEach } from 'vitest'
import { render, screen, cleanup } from '@testing-library/react'
import { OrderTrackingStatus } from './OrderTrackingStatus'
import type { Money, Order } from '../../api/types'

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
    placedAt: '2026-10-22T10:00:00Z',
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

function renderStatus(order: Order) {
  render(<OrderTrackingStatus order={order} isLoading={false} error={null} connectionState="connected" />)
}

describe('OrderTrackingStatus', () => {
  afterEach(() => {
    cleanup()
  })

  it('echoes the requested fulfillment time for a scheduled order', () => {
    renderStatus(makeOrder({ requestedFulfillmentTime: '2026-10-22T16:30:00Z' }))

    expect(screen.getByText(/^Zamówione na:/)).not.toBeNull()
  })

  it('shows no requested-time line for an as-soon-as-possible order', () => {
    renderStatus(makeOrder())

    expect(screen.queryByText(/Zamówione na:/)).toBeNull()
  })
})
