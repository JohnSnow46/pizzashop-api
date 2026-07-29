import { describe, it, expect } from 'vitest'
import { getWindowsForDate, isWithinOpeningHours, isInPast } from './openingHours'
import type { RestaurantConfig, TimeRange } from '../api/types'

const mondayWindows: TimeRange[] = [{ start: '10:00', end: '22:00' }]

const config: RestaurantConfig = {
  id: 'restaurant-1',
  name: 'PizzaShop',
  address: {
    street: 'Main St',
    buildingNumber: '1',
    city: 'Warsaw',
    postalCode: '00-001',
    apartmentNumber: null,
    notes: null,
  },
  location: { latitude: 52.2297, longitude: 21.0122 },
  deliveryRadiusKm: 5,
  timeZoneId: 'Europe/Warsaw',
  openingHours: {
    schedule: {
      Monday: mondayWindows,
      Tuesday: [],
      Wednesday: [],
      Thursday: [],
      Friday: [],
      Saturday: [],
      Sunday: [],
    },
  },
  contactPhone: '123456789',
  isAcceptingOrders: true,
  minimumOrderValue: null,
  freeDeliveryThreshold: null,
  deliveryFee: { amount: 5, currency: 'PLN' },
}

// 2026-07-27 is a Monday, 2026-07-28 is a Tuesday.
const monday = (hours: number, minutes: number) => new Date(2026, 6, 27, hours, minutes)
const tuesday = (hours: number, minutes: number) => new Date(2026, 6, 28, hours, minutes)

describe('getWindowsForDate', () => {
  it('returns the windows configured for that day', () => {
    expect(getWindowsForDate(config, monday(12, 0))).toEqual(mondayWindows)
  })

  it('returns an empty array when that day has no windows configured', () => {
    expect(getWindowsForDate(config, tuesday(12, 0))).toEqual([])
  })
})

describe('isWithinOpeningHours', () => {
  it('returns true for a time inside a window', () => {
    expect(isWithinOpeningHours(config, monday(15, 0))).toBe(true)
  })

  it('returns false for a time outside all windows', () => {
    expect(isWithinOpeningHours(config, monday(8, 0))).toBe(false)
  })

  it('returns true exactly at the window start boundary', () => {
    expect(isWithinOpeningHours(config, monday(10, 0))).toBe(true)
  })

  it('returns true exactly at the window end boundary', () => {
    expect(isWithinOpeningHours(config, monday(22, 0))).toBe(true)
  })
})

describe('isInPast', () => {
  it('returns true for a date clearly in the past', () => {
    expect(isInPast(new Date(2000, 0, 1))).toBe(true)
  })

  it('returns false for a date clearly in the future', () => {
    expect(isInPast(new Date(2100, 0, 1))).toBe(false)
  })
})
