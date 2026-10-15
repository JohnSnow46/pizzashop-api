import { describe, it, expect, afterEach, vi } from 'vitest'
import { render, screen, cleanup } from '@testing-library/react'
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
})
