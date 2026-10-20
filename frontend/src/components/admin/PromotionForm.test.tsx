import { describe, it, expect, afterEach, vi } from 'vitest'
import { render, screen, cleanup } from '@testing-library/react'
import { PromotionForm } from './PromotionForm'

vi.mock('../../api/promotionsApi', () => ({
  createPromotion: vi.fn(),
  updatePromotion: vi.fn(),
}))

describe('PromotionForm', () => {
  afterEach(() => {
    cleanup()
  })

  it('caps name/code at the backend MaximumLength (200/50 chars)', () => {
    render(<PromotionForm mode="create" onSaved={vi.fn()} onCancel={vi.fn()} />)

    expect(screen.getByLabelText('Nazwa').getAttribute('maxlength')).toBe('200')
    expect(screen.getByLabelText('Kod (opcjonalnie)').getAttribute('maxlength')).toBe('50')
  })
})
