import { describe, it, expect, afterEach, vi } from 'vitest'
import { render, screen, cleanup, fireEvent } from '@testing-library/react'
import { MenuItemForm } from './MenuItemForm'
import { updateMenuItem } from '../../api/menuApi'

vi.mock('../../api/menuApi', () => ({
  createMenuItem: vi.fn(),
  updateMenuItem: vi.fn(),
  uploadMenuItemImage: vi.fn(),
}))

describe('MenuItemForm', () => {
  afterEach(() => {
    cleanup()
    vi.restoreAllMocks()
    vi.mocked(updateMenuItem).mockClear()
  })

  it('shows Polish category labels in the category select instead of the raw enum values', () => {
    render(<MenuItemForm mode="create" ingredients={[]} onSaved={vi.fn()} onCancel={vi.fn()} />)

    const select = screen.getByLabelText('Kategoria') as HTMLSelectElement
    const labels = Array.from(select.options).map((o) => o.textContent)
    expect(labels).toEqual(['Pizza', 'Napoje', 'Dodatki', 'Desery', 'Sosy'])
  })

  it('keeps the raw enum value as the option value sent to the backend', () => {
    render(<MenuItemForm mode="create" ingredients={[]} onSaved={vi.fn()} onCancel={vi.fn()} />)

    const select = screen.getByLabelText('Kategoria') as HTMLSelectElement
    expect(Array.from(select.options).map((o) => o.value)).toEqual(['Pizza', 'Drink', 'Side', 'Dessert', 'Sauce'])
  })

  it('caps name/description at the backend MaximumLength (200/1000 chars)', () => {
    render(<MenuItemForm mode="create" ingredients={[]} onSaved={vi.fn()} onCancel={vi.fn()} />)

    expect(screen.getByLabelText('Nazwa').getAttribute('maxlength')).toBe('200')
    expect(screen.getByLabelText('Opis (opcjonalnie)').getAttribute('maxlength')).toBe('1000')
  })

  it('caps each variant name at the backend MaximumLength (100 chars)', () => {
    const item = {
      id: 'pizza-1',
      name: 'Margherita',
      description: null,
      category: 'Pizza' as const,
      basePrice: { amount: 25, currency: 'PLN' },
      isAvailable: true,
      imageUrl: null,
      variants: [{ id: 'v1', name: '32 cm', price: { amount: 25, currency: 'PLN' }, isDefault: true }],
      baseIngredients: [],
      allowedExtras: [],
      averageRating: null,
      reviewCount: 0,
    }
    render(<MenuItemForm mode="edit" item={item} ingredients={[]} onSaved={vi.fn()} onCancel={vi.fn()} />)

    expect(screen.getByPlaceholderText('Nazwa wariantu').getAttribute('maxlength')).toBe('100')
  })

  it('asks before removing the photo and keeps it when declined', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    const item = {
      id: 'pizza-1',
      name: 'Margherita',
      description: null,
      category: 'Pizza' as const,
      basePrice: { amount: 25, currency: 'PLN' },
      isAvailable: true,
      imageUrl: '/images/margherita.webp',
      variants: [],
      baseIngredients: [],
      allowedExtras: [],
      averageRating: null,
      reviewCount: 0,
    }
    render(<MenuItemForm mode="edit" item={item} ingredients={[]} onSaved={vi.fn()} onCancel={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'Usuń zdjęcie' }))

    expect(window.confirm).toHaveBeenCalledOnce()
    expect(updateMenuItem).not.toHaveBeenCalled()
  })
})
