import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'
import { AuthContext } from '../auth/AuthContext'
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

import { getOrderById, getOrderByTrackingToken } from '../api/ordersApi'
import { useOrderTracking } from './useOrderTracking'

const getOrderByIdMock = vi.mocked(getOrderById)
const getOrderByTrackingTokenMock = vi.mocked(getOrderByTrackingToken)

function makeOrder(overrides: Partial<Order> = {}): Order {
  return {
    id: 'abc',
    number: 'ORD-1',
    customerId: null,
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
    ...overrides,
  }
}

function wrapper({ children }: { children: ReactNode }) {
  return (
    <AuthContext.Provider
      value={{
        token: 'test-token',
        user: null,
        isAuthenticated: false,
        isLoading: false,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

beforeEach(() => {
  resetFakeConnection()
  hubConnectionBuilderMock.mockClear()
  getOrderByIdMock.mockReset()
  getOrderByTrackingTokenMock.mockReset()
  getOrderByIdMock.mockResolvedValue(makeOrder())
  getOrderByTrackingTokenMock.mockResolvedValue(makeOrder())
})

describe('useOrderTracking', () => {
  it('source: null does not fetch or connect', () => {
    const { result } = renderHook(() => useOrderTracking(null), { wrapper })

    expect(result.current.isLoading).toBe(false)
    expect(result.current.order).toBeNull()
    expect(result.current.connectionState).toBe('disconnected')
    expect(getOrderByIdMock).not.toHaveBeenCalled()
    expect(getOrderByTrackingTokenMock).not.toHaveBeenCalled()
    expect(hubConnectionBuilderMock).not.toHaveBeenCalled()
  })

  it('source: { orderId } fetches via getOrderById and subscribes via SubscribeToOrder', async () => {
    const { result } = renderHook(() => useOrderTracking({ orderId: 'abc' }), { wrapper })

    await waitFor(() => expect(result.current.connectionState).toBe('connected'))

    expect(getOrderByIdMock).toHaveBeenCalledWith('abc')
    expect(getOrderByTrackingTokenMock).not.toHaveBeenCalled()
    expect(result.current.order).not.toBeNull()
    expect(result.current.isLoading).toBe(false)
    expect(fakeConnection.current.start).toHaveBeenCalled()
    expect(fakeConnection.current.invoke).toHaveBeenCalledWith('SubscribeToOrder', 'abc')
  })

  it('source: { trackingToken } fetches via getOrderByTrackingToken and subscribes via SubscribeToGuestOrder', async () => {
    const { result } = renderHook(() => useOrderTracking({ trackingToken: 'tok' }), { wrapper })

    await waitFor(() => expect(result.current.connectionState).toBe('connected'))

    expect(getOrderByTrackingTokenMock).toHaveBeenCalledWith('tok')
    expect(getOrderByIdMock).not.toHaveBeenCalled()
    expect(fakeConnection.current.invoke).toHaveBeenCalledWith('SubscribeToGuestOrder', 'tok')
  })

  it('applies a pushed OrderStatusChanged event for the matching order', async () => {
    const { result } = renderHook(() => useOrderTracking({ orderId: 'abc' }), { wrapper })

    await waitFor(() => expect(result.current.connectionState).toBe('connected'))

    const onCall = fakeConnection.current.on.mock.calls.find((call) => call[0] === 'OrderStatusChanged')
    expect(onCall).toBeDefined()
    const callback = onCall![1] as (payload: { orderId: string; status: OrderStatus; estimatedReadyAt: string | null }) => void

    callback({ orderId: 'abc', status: 'Ready', estimatedReadyAt: '2026-01-01T00:00:00Z' })

    await waitFor(() => expect(result.current.order?.status).toBe('Ready'))
    expect(result.current.order?.estimatedReadyAt).toBe('2026-01-01T00:00:00Z')
  })

  it('ignores a pushed event for a different orderId', async () => {
    const { result } = renderHook(() => useOrderTracking({ orderId: 'abc' }), { wrapper })

    await waitFor(() => expect(result.current.connectionState).toBe('connected'))

    const onCall = fakeConnection.current.on.mock.calls.find((call) => call[0] === 'OrderStatusChanged')
    const callback = onCall![1] as (payload: { orderId: string; status: OrderStatus; estimatedReadyAt: string | null }) => void
    const statusBefore = result.current.order?.status

    callback({ orderId: 'different-order', status: 'Ready', estimatedReadyAt: '2026-01-01T00:00:00Z' })

    expect(result.current.order?.status).toBe(statusBefore)
  })

  it('sets an error and never builds the connection when the REST fetch rejects', async () => {
    getOrderByIdMock.mockRejectedValue(new Error('not found'))

    const { result } = renderHook(() => useOrderTracking({ orderId: 'missing' }), { wrapper })

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.error).toBe('not found')
    expect(hubConnectionBuilderMock).not.toHaveBeenCalled()
  })

  it('silently ends up disconnected when connection.start() rejects', async () => {
    fakeConnection.current.start.mockRejectedValue(new Error('connection failed'))

    const { result } = renderHook(() => useOrderTracking({ orderId: 'abc' }), { wrapper })

    await waitFor(() => expect(result.current.connectionState).toBe('disconnected'))
    expect(result.current.error).toBeNull()
  })

  it('calls connection.stop() on unmount', async () => {
    const { result, unmount } = renderHook(() => useOrderTracking({ orderId: 'abc' }), { wrapper })

    await waitFor(() => expect(result.current.connectionState).toBe('connected'))

    unmount()

    expect(fakeConnection.current.stop).toHaveBeenCalled()
  })
})
