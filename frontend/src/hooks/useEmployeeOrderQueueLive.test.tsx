import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'
import { AuthContext } from '../auth/AuthContext'

const { fakeConnection, hubConnectionBuilderMock, resetFakeConnection } = vi.hoisted(() => {
  function makeFakeConnection() {
    return {
      on: vi.fn(),
      onreconnected: vi.fn(),
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

import { useEmployeeOrderQueueLive } from './useEmployeeOrderQueueLive'

function wrapper(token: string | null) {
  return function Wrapper({ children }: { children: ReactNode }) {
    return (
      <AuthContext.Provider
        value={{
          token,
          user: null,
          isAuthenticated: token !== null,
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
}

beforeEach(() => {
  resetFakeConnection()
  hubConnectionBuilderMock.mockClear()
})

describe('useEmployeeOrderQueueLive', () => {
  it('connects and calls SubscribeToStaffQueue once authenticated', async () => {
    renderHook(() => useEmployeeOrderQueueLive(vi.fn()), { wrapper: wrapper('staff-token') })

    await waitFor(() => expect(fakeConnection.current.start).toHaveBeenCalled())
    expect(fakeConnection.current.invoke).toHaveBeenCalledWith('SubscribeToStaffQueue')
  })

  it('does not connect when there is no token', () => {
    renderHook(() => useEmployeeOrderQueueLive(vi.fn()), { wrapper: wrapper(null) })

    expect(hubConnectionBuilderMock).not.toHaveBeenCalled()
  })

  it('invokes the callback with the orderId from a NewOrderPlaced push', async () => {
    const onNewOrder = vi.fn()
    renderHook(() => useEmployeeOrderQueueLive(onNewOrder), { wrapper: wrapper('staff-token') })

    await waitFor(() => expect(fakeConnection.current.on).toHaveBeenCalled())

    const onCall = fakeConnection.current.on.mock.calls.find((call) => call[0] === 'NewOrderPlaced')
    expect(onCall).toBeDefined()
    const callback = onCall![1] as (payload: { orderId: string }) => void

    callback({ orderId: 'new-order-1' })

    expect(onNewOrder).toHaveBeenCalledWith('new-order-1')
  })

  it('re-subscribes to the staff group on reconnect', async () => {
    renderHook(() => useEmployeeOrderQueueLive(vi.fn()), { wrapper: wrapper('staff-token') })

    await waitFor(() => expect(fakeConnection.current.onreconnected).toHaveBeenCalled())
    fakeConnection.current.invoke.mockClear()

    const reconnectedCallback = fakeConnection.current.onreconnected.mock.calls[0][0] as () => void
    reconnectedCallback()

    expect(fakeConnection.current.invoke).toHaveBeenCalledWith('SubscribeToStaffQueue')
  })

  it('calls connection.stop() on unmount', async () => {
    const { unmount } = renderHook(() => useEmployeeOrderQueueLive(vi.fn()), { wrapper: wrapper('staff-token') })

    await waitFor(() => expect(fakeConnection.current.start).toHaveBeenCalled())

    unmount()

    expect(fakeConnection.current.stop).toHaveBeenCalled()
  })
})
