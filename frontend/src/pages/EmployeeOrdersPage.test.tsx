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

import { getOrderQueue } from '../api/ordersApi'
import { EmployeeOrdersPage } from './EmployeeOrdersPage'

const getOrderQueueMock = vi.mocked(getOrderQueue)

function makeOrders(ids: string[]): Order[] {
  return ids.map((id) => ({ id }) as unknown as Order)
}

beforeEach(() => {
  getOrderQueueMock.mockReset()
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
})
