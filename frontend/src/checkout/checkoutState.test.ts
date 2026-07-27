import { describe, it, expect } from 'vitest'
import { act, renderHook } from '@testing-library/react'
import { useCheckoutState } from './checkoutState'
import type { Address, DeliveryAvailability } from '../api/types'

const address: Address = {
  street: 'Main St',
  buildingNumber: '1',
  city: 'Warsaw',
  postalCode: '00-001',
  apartmentNumber: null,
  notes: null,
}

const deliveryCheck: DeliveryAvailability = {
  isAvailable: true,
  distanceKm: 1,
  deliveryFee: { amount: 5, currency: 'PLN' },
}

describe('useCheckoutState', () => {
  it('goNext from step 1 with fulfillmentType Delivery goes to step 2', () => {
    const { result } = renderHook(() => useCheckoutState())
    act(() => result.current[1]({ type: 'setFulfillment', fulfillmentType: 'Delivery' }))
    act(() => result.current[1]({ type: 'goNext' }))
    expect(result.current[0].step).toBe(2)
  })

  it('goNext from step 1 with fulfillmentType Pickup skips to step 3', () => {
    const { result } = renderHook(() => useCheckoutState())
    act(() => result.current[1]({ type: 'setFulfillment', fulfillmentType: 'Pickup' }))
    act(() => result.current[1]({ type: 'goNext' }))
    expect(result.current[0].step).toBe(3)
  })

  it('goNext from step 1 with fulfillmentType null skips to step 3', () => {
    const { result } = renderHook(() => useCheckoutState())
    act(() => result.current[1]({ type: 'goNext' }))
    expect(result.current[0].step).toBe(3)
  })

  it('goBack from step 3 with Delivery goes back to step 2', () => {
    const { result } = renderHook(() => useCheckoutState())
    act(() => result.current[1]({ type: 'setFulfillment', fulfillmentType: 'Delivery' }))
    act(() => result.current[1]({ type: 'goToStep', step: 3 }))
    act(() => result.current[1]({ type: 'goBack' }))
    expect(result.current[0].step).toBe(2)
  })

  it('goBack from step 3 with Pickup skips back to step 1', () => {
    const { result } = renderHook(() => useCheckoutState())
    act(() => result.current[1]({ type: 'setFulfillment', fulfillmentType: 'Pickup' }))
    act(() => result.current[1]({ type: 'goToStep', step: 3 }))
    act(() => result.current[1]({ type: 'goBack' }))
    expect(result.current[0].step).toBe(1)
  })

  it('goNext clamps at step 7', () => {
    const { result } = renderHook(() => useCheckoutState())
    act(() => result.current[1]({ type: 'goToStep', step: 7 }))
    act(() => result.current[1]({ type: 'goNext' }))
    expect(result.current[0].step).toBe(7)
  })

  it('goBack clamps at step 1', () => {
    const { result } = renderHook(() => useCheckoutState())
    act(() => result.current[1]({ type: 'goBack' }))
    expect(result.current[0].step).toBe(1)
  })

  it('setFulfillment to Pickup after address/deliveryCheck were set clears both', () => {
    const { result } = renderHook(() => useCheckoutState())
    act(() => result.current[1]({ type: 'setFulfillment', fulfillmentType: 'Delivery' }))
    act(() => result.current[1]({ type: 'setAddress', address }))
    act(() => result.current[1]({ type: 'setDeliveryCheck', result: deliveryCheck }))
    expect(result.current[0].address).toEqual(address)
    expect(result.current[0].deliveryCheck).toEqual(deliveryCheck)

    act(() => result.current[1]({ type: 'setFulfillment', fulfillmentType: 'Pickup' }))
    expect(result.current[0].address).toBeNull()
    expect(result.current[0].deliveryCheck).toBeNull()
  })

  it('setFulfillment to the same fulfillmentType is a no-op (preserves address/deliveryCheck)', () => {
    const { result } = renderHook(() => useCheckoutState())
    act(() => result.current[1]({ type: 'setFulfillment', fulfillmentType: 'Delivery' }))
    act(() => result.current[1]({ type: 'setAddress', address }))
    act(() => result.current[1]({ type: 'setDeliveryCheck', result: deliveryCheck }))

    const stateBefore = result.current[0]
    act(() => result.current[1]({ type: 'setFulfillment', fulfillmentType: 'Delivery' }))
    expect(result.current[0]).toBe(stateBefore)
    expect(result.current[0].address).toEqual(address)
    expect(result.current[0].deliveryCheck).toEqual(deliveryCheck)
  })

  it('goToStep jumps directly regardless of current step', () => {
    const { result } = renderHook(() => useCheckoutState())
    act(() => result.current[1]({ type: 'goToStep', step: 5 }))
    expect(result.current[0].step).toBe(5)
    act(() => result.current[1]({ type: 'goToStep', step: 2 }))
    expect(result.current[0].step).toBe(2)
  })
})
