import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, waitFor, cleanup } from '@testing-library/react'
import { ApiError } from '../../api/client'
import { RetryPaymentButton } from './RetryPaymentButton'

vi.mock('../../api/paymentsApi', () => ({
  initializePayment: vi.fn(),
  initializeGuestPayment: vi.fn(),
}))

import { initializePayment, initializeGuestPayment } from '../../api/paymentsApi'

const initializePaymentMock = vi.mocked(initializePayment)
const initializeGuestPaymentMock = vi.mocked(initializeGuestPayment)

beforeEach(() => {
  initializePaymentMock.mockReset()
  initializeGuestPaymentMock.mockReset()
  // @ts-expect-error jsdom does not implement navigation; stub the setter for assertions.
  delete window.location
  // @ts-expect-error see above
  window.location = { href: '' }
})

afterEach(() => {
  cleanup()
})

describe('RetryPaymentButton', () => {
  it('source: { orderId } calls initializePayment and redirects to the returned URL', async () => {
    initializePaymentMock.mockResolvedValue({ redirectUrl: 'https://payu.example/session/1' })

    render(<RetryPaymentButton source={{ orderId: 'order-1' }} />)
    fireEvent.click(screen.getByRole('button', { name: 'Zapłać ponownie' }))

    await waitFor(() => expect(window.location.href).toBe('https://payu.example/session/1'))
    expect(initializePaymentMock).toHaveBeenCalledWith('order-1')
    expect(initializeGuestPaymentMock).not.toHaveBeenCalled()
  })

  it('source: { trackingToken } calls initializeGuestPayment and redirects to the returned URL', async () => {
    initializeGuestPaymentMock.mockResolvedValue({ redirectUrl: 'https://payu.example/session/2' })

    render(<RetryPaymentButton source={{ trackingToken: 'tok-1' }} />)
    fireEvent.click(screen.getByRole('button', { name: 'Zapłać ponownie' }))

    await waitFor(() => expect(window.location.href).toBe('https://payu.example/session/2'))
    expect(initializeGuestPaymentMock).toHaveBeenCalledWith('tok-1')
    expect(initializePaymentMock).not.toHaveBeenCalled()
  })

  it('shows the ProblemDetails message and re-enables the button on a 409 conflict', async () => {
    initializeGuestPaymentMock.mockRejectedValue(new ApiError(409, 'Conflict', { title: 'Conflict', detail: 'Zamówienie zostało już opłacone.' }))

    render(<RetryPaymentButton source={{ trackingToken: 'tok-1' }} />)
    fireEvent.click(screen.getByRole('button', { name: 'Zapłać ponownie' }))

    await screen.findByText('Zamówienie zostało już opłacone.')
    expect(window.location.href).toBe('')
    expect((screen.getByRole('button', { name: 'Zapłać ponownie' }) as HTMLButtonElement).disabled).toBe(false)
  })

  it('shows a fallback message for a non-ApiError failure', async () => {
    initializePaymentMock.mockRejectedValue(new Error('network down'))

    render(<RetryPaymentButton source={{ orderId: 'order-1' }} />)
    fireEvent.click(screen.getByRole('button', { name: 'Zapłać ponownie' }))

    await screen.findByText('Nie udało się zainicjować płatności. Spróbuj ponownie.')
  })
})
