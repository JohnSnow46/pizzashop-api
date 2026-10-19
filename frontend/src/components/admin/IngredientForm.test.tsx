import { describe, it, expect, afterEach, vi } from 'vitest'
import { render, screen, cleanup } from '@testing-library/react'
import { IngredientForm } from './IngredientForm'

vi.mock('../../api/ingredientsApi', () => ({
  createIngredient: vi.fn(),
  updateIngredient: vi.fn(),
}))

describe('IngredientForm', () => {
  afterEach(() => {
    cleanup()
  })

  it('caps name/category at the backend MaximumLength (200/100 chars)', () => {
    render(<IngredientForm mode="create" onSaved={vi.fn()} onCancel={vi.fn()} />)

    expect(screen.getByLabelText('Nazwa').getAttribute('maxlength')).toBe('200')
    expect(screen.getByLabelText('Kategoria (opcjonalnie)').getAttribute('maxlength')).toBe('100')
  })
})
