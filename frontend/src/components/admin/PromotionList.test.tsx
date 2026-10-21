import { describe, it, expect, afterEach, vi } from 'vitest'
import { render, screen, cleanup } from '@testing-library/react'
import { PromotionList } from './PromotionList'
import { promotionStatus } from './promotionStatus'
import type { Promotion } from '../../api/types'

vi.mock('../../api/promotionsApi', () => ({
  deactivatePromotion: vi.fn(),
}))

function makePromotion(overrides: Partial<Promotion> = {}): Promotion {
  return {
    id: 'promo-1',
    name: 'Jesień',
    code: 'JESIEN10',
    type: 'Percentage',
    value: 10,
    minOrderValue: null,
    validFrom: '2026-10-01T00:00:00Z',
    validTo: '2026-10-31T23:59:59Z',
    isActive: true,
    usageLimit: null,
    usageCount: 0,
    buyXGetY: null,
    ...overrides,
  }
}

const now = new Date('2026-10-15T12:00:00Z')

describe('promotionStatus', () => {
  it('is "Aktywna" inside the window with no usage limit hit', () => {
    expect(promotionStatus(makePromotion(), now)).toBe('Aktywna')
  })

  it('is "Nieaktywna" once deactivated, regardless of the window', () => {
    expect(promotionStatus(makePromotion({ isActive: false }), now)).toBe('Nieaktywna')
  })

  it('is "Zaplanowana" before validFrom', () => {
    expect(promotionStatus(makePromotion({ validFrom: '2026-10-20T00:00:00Z' }), now)).toBe('Zaplanowana')
  })

  it('is "Wygasła" after validTo', () => {
    expect(promotionStatus(makePromotion({ validTo: '2026-10-10T00:00:00Z' }), now)).toBe('Wygasła')
  })

  it('is "Wyczerpana" once usageCount reaches usageLimit', () => {
    expect(promotionStatus(makePromotion({ usageLimit: 5, usageCount: 5 }), now)).toBe('Wyczerpana')
  })
})

describe('PromotionList', () => {
  afterEach(() => {
    cleanup()
  })

  it('renders a Status column instead of the raw isActive flag', () => {
    render(<PromotionList promotions={[makePromotion({ isActive: false })]} onEdit={vi.fn()} onDeactivated={vi.fn()} />)

    expect(screen.getByText('Status')).not.toBeNull()
    expect(screen.getByText('Nieaktywna')).not.toBeNull()
  })
})
