import { describe, it, expect, vi, afterEach } from 'vitest'
import { render, screen, fireEvent, cleanup } from '@testing-library/react'
import { ApiError } from '../../api/client'
import { validatePromotion } from '../../api/promotionsApi'
import { PromotionField } from './PromotionField'

vi.mock('../../api/promotionsApi', () => ({ validatePromotion: vi.fn() }))

const money = { amount: 0, currency: 'PLN' }

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('PromotionField', () => {
  it('marks a rejected code as invalid and links the error to the input', async () => {
    vi.mocked(validatePromotion).mockRejectedValue(new ApiError(404, 'Not found', { title: 'Not found', detail: 'Nie znaleziono kodu.' }))
    render(
      <PromotionField
        code={null}
        preview={null}
        subtotal={money}
        deliveryFee={money}
        lines={[]}
        onApply={vi.fn()}
        onNext={vi.fn()}
        onBack={vi.fn()}
      />,
    )

    const input = screen.getByLabelText('Kod (opcjonalnie)')
    expect(input.getAttribute('aria-invalid')).toBe('false')
    expect(input.hasAttribute('aria-describedby')).toBe(false)

    fireEvent.change(input, { target: { value: 'NOPE' } })
    fireEvent.click(screen.getByText('Sprawdź kod'))
    await screen.findByText('Nie znaleziono kodu.')

    expect(input.getAttribute('aria-invalid')).toBe('true')
    expect(document.getElementById(input.getAttribute('aria-describedby')!)?.textContent).toBe('Nie znaleziono kodu.')
  })
})
