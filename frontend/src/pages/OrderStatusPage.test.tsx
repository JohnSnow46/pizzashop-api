import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, waitFor, cleanup } from '@testing-library/react'
import { MemoryRouter, Routes, Route, Link } from 'react-router-dom'
import { StrictMode } from 'react'
import { AuthContext, type AuthContextValue } from '../auth/AuthContext'
import type { Order, OrderStatus } from '../api/types'

const { fakeConnection, hubConnectionBuilderMock, resetFakeConnection } = vi.hoisted(() => {
  function makeFakeConnection() {
    return {
      on: vi.fn(),
      onreconnecting: vi.fn(),
      onreconnected: vi.fn(),
      onclose: vi.fn(),
      start: vi.fn().mockResolvedValue(undefined),
      invoke: vi.fn().mockResolvedValue(undefined),
      stop: vi.fn().mockResolvedValue(undefined),
    }
  }

  const fakeConnection = { current: makeFakeConnection() }

  function resetFakeConnection() {
    fakeConnection.current = makeFakeConnection()
  }

  const hubConnectionBuilderMock = vi.fn()

  return { fakeConnection, hubConnectionBuilderMock, resetFakeConnection }
})

vi.mock('@microsoft/signalr', () => {
  class HubConnectionBuilder {
    withUrl(...args: unknown[]) {
      hubConnectionBuilderMock('withUrl', ...args)
      return this
    }
    withAutomaticReconnect(...args: unknown[]) {
      hubConnectionBuilderMock('withAutomaticReconnect', ...args)
      return this
    }
    build() {
      hubConnectionBuilderMock('build')
      return fakeConnection.current
    }
  }
  return { HubConnectionBuilder }
})

vi.mock('../api/ordersApi', () => ({
  getOrderById: vi.fn(),
  getOrderByTrackingToken: vi.fn(),
}))

import { getOrderById } from '../api/ordersApi'
import { OrderStatusPage } from './OrderStatusPage'

const getOrderByIdMock = vi.mocked(getOrderById)

function makeOrder(id: string): Order {
  return {
    id,
    number: `ORD-${id}`,
    customerId: 'cust-1',
    contact: { fullName: 'Jan Kowalski', phoneNumber: '123456789', email: null },
    fulfillmentType: 'Pickup',
    deliveryAddress: null,
    placedAt: '2026-01-01T00:00:00Z',
    requestedFulfillmentTime: null,
    estimatedReadyAt: null,
    status: 'PendingAcceptance' as OrderStatus,
    paymentMethod: 'OnPickup',
    paymentStatus: 'Pending',
    subtotal: { amount: 0, currency: 'PLN' },
    discountAmount: { amount: 0, currency: 'PLN' },
    deliveryFee: { amount: 0, currency: 'PLN' },
    total: { amount: 0, currency: 'PLN' },
    items: [],
  }
}

const authValue: AuthContextValue = {
  user: { userAccountId: 'u1', email: 'a@b.pl', role: 'Customer', customerId: 'c1', fullName: null },
  token: 'test-token',
  isAuthenticated: true,
  isLoading: false,
  login: vi.fn(),
  register: vi.fn(),
  logout: vi.fn(),
}

/** Stand-in for the relevant slice of `/account` (a plain history list of `<Link>`s) — the
 * real MyAccountPage pulls in loyalty/address APIs that are irrelevant to this navigation bug. */
function ListPage() {
  return (
    <div>
      <Link to="/account/orders/order-1">Zamówienie 1</Link>
      <Link to="/account/orders/order-2">Zamówienie 2</Link>
    </div>
  )
}

/** Mirrors the always-visible "Moje konto" header link in Layout.tsx — the real way a user
 * goes back from an order status page to the order history list. */
function PersistentNav() {
  return (
    <nav>
      <Link to="/account">Moje konto</Link>
    </nav>
  )
}

function renderApp(wrapStrict: boolean) {
  const tree = (
    <AuthContext.Provider value={authValue}>
      <MemoryRouter initialEntries={['/account']}>
        <PersistentNav />
        <Routes>
          <Route path="/account" element={<ListPage />} />
          <Route path="/account/orders/:orderId" element={<OrderStatusPage />} />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>
  )

  return render(wrapStrict ? <StrictMode>{tree}</StrictMode> : tree)
}

beforeEach(() => {
  resetFakeConnection()
  hubConnectionBuilderMock.mockClear()
  getOrderByIdMock.mockReset()
  getOrderByIdMock.mockImplementation((orderId: string) => Promise.resolve(makeOrder(orderId)))
})

afterEach(() => {
  cleanup()
})

describe('OrderStatusPage — navigation from /account', () => {
  it('fetches and shows the clicked order (fresh mount), not the "not found" empty state', async () => {
    renderApp(false)

    fireEvent.click(screen.getByText('Zamówienie 1'))

    await waitFor(() => expect(screen.getByText('ORD-order-1')).toBeDefined())
    expect(screen.queryByText('Nie znaleziono zamówienia.')).toBeNull()
    expect(getOrderByIdMock).toHaveBeenCalledWith('order-1')
  })

  it('under StrictMode (dev double-invoke), still resolves to the correct order without a false not-found', async () => {
    renderApp(true)

    fireEvent.click(screen.getByText('Zamówienie 2'))

    await waitFor(() => expect(screen.getByText('ORD-order-2')).toBeDefined())
    expect(screen.queryByText('Nie znaleziono zamówienia.')).toBeNull()
    // StrictMode double-invokes the mount effect in dev, so getOrderById may fire twice for
    // the same id — both calls are for the correct id (see useOrderTracking.ts comment).
    for (const call of getOrderByIdMock.mock.calls) {
      expect(call[0]).toBe('order-2')
    }
  })

  it('10 rapid round trips /account -> /account/orders/:id never show a false not-found', async () => {
    renderApp(true)

    for (let i = 0; i < 10; i++) {
      const target = i % 2 === 0 ? 'order-1' : 'order-2'
      const label = target === 'order-1' ? 'Zamówienie 1' : 'Zamówienie 2'

      fireEvent.click(screen.getByText(label))
      await waitFor(() => expect(screen.getByText(`ORD-${target}`)).toBeDefined())
      expect(screen.queryByText('Nie znaleziono zamówienia.')).toBeNull()

      // Navigate back to the list (via the always-present header link) before the next round trip.
      fireEvent.click(screen.getByText('Moje konto'))
      await waitFor(() => expect(screen.getByText('Zamówienie 1')).toBeDefined())
    }
  })

  it('resolves correctly even when an in-flight (stale) fetch settles after the current one', async () => {
    let resolveFirst!: (order: Order) => void
    let resolveSecond!: (order: Order) => void

    getOrderByIdMock
      .mockImplementationOnce(() => new Promise((resolve) => (resolveFirst = resolve)))
      .mockImplementationOnce(() => new Promise((resolve) => (resolveSecond = resolve)))

    renderApp(true)
    fireEvent.click(screen.getByText('Zamówienie 1'))

    await waitFor(() => expect(getOrderByIdMock.mock.calls.length).toBeGreaterThanOrEqual(1))

    // Resolve whichever StrictMode invocation settles "second" first, simulating real-world
    // network non-determinism where request order and response order can differ.
    resolveSecond(makeOrder('order-1'))
    await waitFor(() => expect(screen.getByText('ORD-order-1')).toBeDefined())
    resolveFirst(makeOrder('order-1'))

    expect(screen.queryByText('Nie znaleziono zamówienia.')).toBeNull()
  })
})
