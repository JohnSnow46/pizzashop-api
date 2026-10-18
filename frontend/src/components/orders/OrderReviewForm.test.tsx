import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, waitFor, cleanup } from '@testing-library/react'
import { OrderReviewForm } from './OrderReviewForm'
import type { Order, OrderItem, Money } from '../../api/types'

vi.mock('../../api/reviewsApi', () => ({
  getMyReviews: vi.fn(),
  createReview: vi.fn(),
}))

import { getMyReviews, createReview } from '../../api/reviewsApi'

const getMyReviewsMock = vi.mocked(getMyReviews)
const createReviewMock = vi.mocked(createReview)

function money(amount: number): Money {
  return { amount, currency: 'PLN' }
}

function makeItem(overrides: Partial<OrderItem> = {}): OrderItem {
  return {
    id: 'item-1',
    menuItemId: 'menu-1',
    menuItemName: 'Margherita',
    variantId: null,
    variantName: null,
    unitPrice: money(25),
    quantity: 1,
    notes: null,
    extras: [],
    lineTotal: money(25),
    ...overrides,
  }
}

function makeOrder(items: OrderItem[]): Order {
  return {
    id: 'order-1',
    number: '1001',
    customerId: 'customer-1',
    contact: { fullName: 'Jan Kowalski', phoneNumber: '123456789', email: null },
    fulfillmentType: 'Pickup',
    deliveryAddress: null,
    placedAt: new Date().toISOString(),
    requestedFulfillmentTime: null,
    estimatedReadyAt: null,
    status: 'Completed',
    paymentMethod: 'Online',
    paymentStatus: 'Paid',
    subtotal: money(25),
    discountAmount: money(0),
    deliveryFee: money(0),
    total: money(25),
    items,
  }
}

beforeEach(() => {
  getMyReviewsMock.mockReset()
  createReviewMock.mockReset()
})

afterEach(() => {
  cleanup()
})

describe('OrderReviewForm', () => {
  it('caps the comment textarea at the backend MaxCommentLength (1000 chars)', async () => {
    getMyReviewsMock.mockResolvedValue([])
    render(<OrderReviewForm order={makeOrder([makeItem()])} />)

    const textarea = await screen.findByPlaceholderText('Komentarz (opcjonalnie)')
    expect(textarea.getAttribute('maxlength')).toBe('1000')
  })

  it('submits a review with the trimmed comment and rating', async () => {
    getMyReviewsMock.mockResolvedValue([])
    createReviewMock.mockResolvedValue('review-1')
    render(<OrderReviewForm order={makeOrder([makeItem()])} />)

    fireEvent.click(await screen.findByText('5★'))
    fireEvent.change(screen.getByPlaceholderText('Komentarz (opcjonalnie)'), { target: { value: '  Great pizza!  ' } })
    fireEvent.click(screen.getByText('Wyślij recenzję'))

    await waitFor(() =>
      expect(createReviewMock).toHaveBeenCalledWith({
        orderId: 'order-1',
        menuItemId: 'menu-1',
        rating: 5,
        comment: 'Great pizza!',
      }),
    )
  })

  it('shows a thank-you message once every item has already been reviewed', async () => {
    getMyReviewsMock.mockResolvedValue([
      { id: 'r1', orderId: 'order-1', customerId: 'customer-1', menuItemId: 'menu-1', rating: 4, comment: null, createdAt: new Date().toISOString() },
    ])
    render(<OrderReviewForm order={makeOrder([makeItem()])} />)

    await screen.findByText('Dziękujemy za ocenienie tego zamówienia!')
  })
})
