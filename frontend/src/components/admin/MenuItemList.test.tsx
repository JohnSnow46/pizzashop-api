import { describe, it, expect, afterEach, vi } from 'vitest'
import { render, screen, cleanup, fireEvent, within } from '@testing-library/react'
import { MenuItemList } from './MenuItemList'
import type { MenuItem } from '../../api/types'

function makeItem(overrides: Partial<MenuItem> = {}): MenuItem {
  return {
    id: 'pizza-1',
    name: 'Margherita',
    description: 'Klasyczna pizza z sosem pomidorowym i mozzarellą',
    category: 'Pizza',
    basePrice: { amount: 25, currency: 'PLN' },
    isAvailable: true,
    imageUrl: null,
    variants: [],
    baseIngredients: [],
    allowedExtras: [],
    averageRating: null,
    reviewCount: 0,
    ...overrides,
  }
}

describe('MenuItemList', () => {
  afterEach(() => {
    cleanup()
  })

  it('shows an em dash for an item with no reviews yet', () => {
    render(
      <MenuItemList items={[makeItem({ averageRating: null, reviewCount: 0 })]} onEdit={vi.fn()} onAvailabilityChanged={vi.fn()} />,
    )

    expect(screen.getByText('—')).not.toBeNull()
  })

  it('shows the average rating and review count for a reviewed item', () => {
    render(
      <MenuItemList
        items={[makeItem({ averageRating: 4.5, reviewCount: 12 })]}
        onEdit={vi.fn()}
        onAvailabilityChanged={vi.fn()}
      />,
    )

    expect(screen.getByText('★ 4.5 (12)')).not.toBeNull()
  })

  it('shows the Polish category label instead of the raw enum value', () => {
    render(
      <MenuItemList items={[makeItem({ category: 'Drink' })]} onEdit={vi.fn()} onAvailabilityChanged={vi.fn()} />,
    )

    const table = within(screen.getByRole('table'))
    expect(table.getByText('Napoje')).not.toBeNull()
    expect(table.queryByText('Drink')).toBeNull()
  })

  it('filters by name, case-insensitively', () => {
    render(
      <MenuItemList
        items={[makeItem({ id: 'p1', name: 'Margherita' }), makeItem({ id: 'p2', name: 'Pepperoni' })]}
        onEdit={vi.fn()}
        onAvailabilityChanged={vi.fn()}
      />,
    )

    fireEvent.change(screen.getByLabelText('Szukaj w menu'), { target: { value: 'pepper' } })

    expect(screen.queryByText('Margherita')).toBeNull()
    expect(screen.getByText('Pepperoni')).not.toBeNull()
  })

  it('filters by category', () => {
    render(
      <MenuItemList
        items={[
          makeItem({ id: 'p1', name: 'Margherita', category: 'Pizza' }),
          makeItem({ id: 'p2', name: 'Cola', category: 'Drink' }),
        ]}
        onEdit={vi.fn()}
        onAvailabilityChanged={vi.fn()}
      />,
    )

    fireEvent.change(screen.getByLabelText('Filtruj po kategorii'), { target: { value: 'Drink' } })

    expect(screen.queryByText('Margherita')).toBeNull()
    expect(screen.getByText('Cola')).not.toBeNull()
  })

  it('shows an empty-state message when the filter matches nothing', () => {
    render(
      <MenuItemList items={[makeItem({ name: 'Margherita' })]} onEdit={vi.fn()} onAvailabilityChanged={vi.fn()} />,
    )

    fireEvent.change(screen.getByLabelText('Szukaj w menu'), { target: { value: 'nonexistent' } })

    expect(screen.getByText('Brak pozycji spełniających kryteria wyszukiwania.')).not.toBeNull()
  })
})
