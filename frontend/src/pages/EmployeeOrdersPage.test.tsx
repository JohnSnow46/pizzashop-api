import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, waitFor, cleanup } from '@testing-library/react'
import type { Order } from '../api/types'

vi.mock('../api/ordersApi', () => ({
  getOrderQueue: vi.fn(),
}))

vi.mock('../components/orders/EmployeeOrderRow', () => ({
  EmployeeOrderRow: ({ orderId, onLeftQueue }: { orderId: string; onLeftQueue: (id: string) => void }) => (
    <li>
      <span>{orderId}</span>
      <button onClick={() => onLeftQueue(orderId)}>{`leave-${orderId}`}</button>
    </li>
  ),
}))

// The live "new order" push (ADR-0038 extension) needs an AuthProvider/SignalR connection —
// out of scope for these tests, which only cover the queue's own state/UI reaction to a push.
// The hook's own connect/subscribe/event-wiring behavior is covered by
// useEmployeeOrderQueueLive.test.tsx.
const useEmployeeOrderQueueLiveMock = vi.fn()
vi.mock('../hooks/useEmployeeOrderQueueLive', () => ({
  useEmployeeOrderQueueLive: (onNewOrder: (orderId: string) => void) => useEmployeeOrderQueueLiveMock(onNewOrder),
}))

import { getOrderQueue } from '../api/ordersApi'
import { EmployeeOrdersPage } from './EmployeeOrdersPage'

const getOrderQueueMock = vi.mocked(getOrderQueue)

function makeOrders(ids: string[]): Order[] {
  return ids.map((id) => ({ id }) as unknown as Order)
}

beforeEach(() => {
  getOrderQueueMock.mockReset()
  useEmployeeOrderQueueLiveMock.mockReset()
  // jsdom doesn't implement HTMLMediaElement.play(); EmployeeOrdersPage calls it (with .catch)
  // on every new-order push.
  window.HTMLMediaElement.prototype.play = vi.fn().mockResolvedValue(undefined)
})

afterEach(() => {
  cleanup()
})

describe('EmployeeOrdersPage', () => {
  it('renders a row per queued order id', async () => {
    getOrderQueueMock.mockResolvedValue(makeOrders(['a', 'b']))

    render(<EmployeeOrdersPage />)

    expect(await screen.findByText('a')).toBeDefined()
    expect(screen.getByText('b')).toBeDefined()
  })

  it('shows an empty-state message when the queue has no orders', async () => {
    getOrderQueueMock.mockResolvedValue([])

    render(<EmployeeOrdersPage />)

    expect(await screen.findByText('Brak zamówień w kolejce.')).toBeDefined()
  })

  it('shows an error message when fetching the queue fails', async () => {
    getOrderQueueMock.mockRejectedValue(new Error('Nie udało się pobrać kolejki zamówień.'))

    render(<EmployeeOrdersPage />)

    expect(await screen.findByText('Nie udało się pobrać kolejki zamówień.')).toBeDefined()
  })

  it('removes an order that leaves the queue and merges in newly discovered orders from the refetch', async () => {
    getOrderQueueMock.mockResolvedValueOnce(makeOrders(['a', 'b'])).mockResolvedValueOnce(makeOrders(['b', 'c']))

    render(<EmployeeOrdersPage />)
    await screen.findByText('a')
    await screen.findByText('b')

    fireEvent.click(screen.getByText('leave-a'))

    await waitFor(() => expect(screen.queryByText('a')).toBeNull())
    await screen.findByText('c')
    expect(screen.getByText('b')).toBeDefined()
    expect(getOrderQueueMock).toHaveBeenCalledTimes(2)
  })

  it('adds a live-pushed new order to the queue, shows a badge and plays the beep', async () => {
    getOrderQueueMock.mockResolvedValue(makeOrders(['a']))

    render(<EmployeeOrdersPage />)
    await screen.findByText('a')

    expect(useEmployeeOrderQueueLiveMock).toHaveBeenCalled()
    const onNewOrder = useEmployeeOrderQueueLiveMock.mock.calls.at(-1)![0] as (orderId: string) => void

    onNewOrder('z')

    expect(await screen.findByText('z')).toBeDefined()
    await waitFor(() => expect(screen.getByText('1 nowe zamówienie')).toBeDefined())
    expect(window.HTMLMediaElement.prototype.play).toHaveBeenCalled()
  })

  it('does not duplicate an order that is pushed live but already in the queue', async () => {
    getOrderQueueMock.mockResolvedValue(makeOrders(['a']))

    render(<EmployeeOrdersPage />)
    await screen.findByText('a')

    const onNewOrder = useEmployeeOrderQueueLiveMock.mock.calls.at(-1)![0] as (orderId: string) => void
    onNewOrder('a')

    await waitFor(() => expect(screen.getByText('1 nowe zamówienie')).toBeDefined())
    expect(screen.getAllByText('a')).toHaveLength(1)
  })
})
