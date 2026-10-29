import { describe, it, expect, afterEach, vi } from 'vitest'
import { render, screen, cleanup, fireEvent } from '@testing-library/react'
import { PromotionList } from './PromotionList'
import { deactivatePromotion } from '../../api/promotionsApi'
import { promotionStatus } from './promotionStatus'
import type { Promotion } from '../../api/types'

vi.mock('../../api/promotionsApi', () => ({
  deactivatePromotion: vi.fn().mockResolvedValue(undefined),
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
    vi.clearAllMocks()
  })

  it('renders a Status column instead of the raw isActive flag', () => {
    render(<PromotionList promotions={[makePromotion({ isActive: false })]} onEdit={vi.fn()} onDeactivated={vi.fn()} />)

    expect(screen.getByText('Status')).not.toBeNull()
    expect(screen.getByText('Nieaktywna')).not.toBeNull()
  })

  it('describes a BuyXGetY rule instead of an em dash', () => {
    render(
      <PromotionList
        promotions={[
          makePromotion({
            type: 'BuyXGetY',
            value: null,
            buyXGetY: { triggerMenuItemId: 'm1', buyQuantity: 2, rewardMenuItemId: 'm2', getQuantity: 1, rewardDiscountPercentage: 100 },
          }),
        ]}
        onEdit={vi.fn()}
        onDeactivated={vi.fn()}
      />,
    )

    expect(screen.getByText('Kup 2, 1 taniej o 100%')).not.toBeNull()
  })

  it('appends the minimum order value when one is set', () => {
    render(
      <PromotionList
        promotions={[
          makePromotion({ value: 10, minOrderValue: { amount: 50, currency: 'PLN' } }),
          makePromotion({ id: 'promo-2', type: 'FreeDelivery', value: null, minOrderValue: { amount: 80, currency: 'PLN' } }),
        ]}
        onEdit={vi.fn()}
        onDeactivated={vi.fn()}
      />,
    )

    expect(screen.getByText('10%, od 50.00 PLN')).not.toBeNull()
    expect(screen.getByText('od 80.00 PLN')).not.toBeNull()
  })

  it('filters rows by name or code', () => {
    render(
      <PromotionList
        promotions={[
          makePromotion(),
          makePromotion({ id: 'promo-2', name: 'Weekend', code: 'WEEKEND5' }),
        ]}
        onEdit={vi.fn()}
        onDeactivated={vi.fn()}
      />,
    )

    fireEvent.change(screen.getByLabelText('Szukaj promocji'), { target: { value: 'weekend5' } })
    expect(screen.getByText('Weekend')).not.toBeNull()
    expect(screen.queryByText('Jesień')).toBeNull()

    fireEvent.change(screen.getByLabelText('Szukaj promocji'), { target: { value: 'jesie' } })
    expect(screen.getByText('Jesień')).not.toBeNull()
    expect(screen.queryByText('Weekend')).toBeNull()
  })

  it('says so when the search matches nothing', () => {
    render(<PromotionList promotions={[makePromotion()]} onEdit={vi.fn()} onDeactivated={vi.fn()} />)

    fireEvent.change(screen.getByLabelText('Szukaj promocji'), { target: { value: 'zima' } })

    expect(screen.getByText('Brak promocji spełniających kryteria wyszukiwania.')).not.toBeNull()
    expect(screen.queryByRole('table')).toBeNull()
  })

  it('asks for confirmation before deactivating and does nothing when declined', () => {
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false)
    render(<PromotionList promotions={[makePromotion()]} onEdit={vi.fn()} onDeactivated={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'Dezaktywuj' }))

    expect(confirmSpy).toHaveBeenCalledOnce()
    expect(deactivatePromotion).not.toHaveBeenCalled()
    confirmSpy.mockRestore()
  })

  it('deactivates once confirmed', async () => {
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    const onDeactivated = vi.fn()
    render(<PromotionList promotions={[makePromotion()]} onEdit={vi.fn()} onDeactivated={onDeactivated} />)

    fireEvent.click(screen.getByRole('button', { name: 'Dezaktywuj' }))

    expect(deactivatePromotion).toHaveBeenCalledWith('promo-1')
    await vi.waitFor(() => expect(onDeactivated).toHaveBeenCalled())
    confirmSpy.mockRestore()
  })
})
