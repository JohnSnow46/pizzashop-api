import { describe, it, expect, afterEach, vi } from 'vitest'
import { render, screen, cleanup, fireEvent } from '@testing-library/react'
import { IngredientsSection } from './IngredientsSection'
import type { Ingredient } from '../../api/types'

vi.mock('../../api/ingredientsApi', () => ({
  createIngredient: vi.fn(),
  updateIngredient: vi.fn(),
}))

function ingredient(id: string, name: string, category: string | null): Ingredient {
  return { id, name, category, extraPrice: { amount: 3, currency: 'PLN' }, isAvailable: true }
}

const ingredients = [
  ingredient('i1', 'Mozzarella', 'Sery'),
  ingredient('i2', 'Pieczarki', 'Warzywa'),
  ingredient('i3', 'Salami', null),
]

describe('IngredientsSection', () => {
  afterEach(() => {
    cleanup()
  })

  it('filters by name, case-insensitively', () => {
    render(<IngredientsSection ingredients={ingredients} loading={false} error={null} onChanged={vi.fn()} />)

    fireEvent.change(screen.getByLabelText('Szukaj składnika'), { target: { value: 'mozz' } })

    expect(screen.getByText('Mozzarella')).not.toBeNull()
    expect(screen.queryByText('Pieczarki')).toBeNull()
  })

  it('also matches the category', () => {
    render(<IngredientsSection ingredients={ingredients} loading={false} error={null} onChanged={vi.fn()} />)

    fireEvent.change(screen.getByLabelText('Szukaj składnika'), { target: { value: 'warzyw' } })

    expect(screen.getByText('Pieczarki')).not.toBeNull()
    expect(screen.queryByText('Mozzarella')).toBeNull()
    expect(screen.queryByText('Salami')).toBeNull()
  })
})
