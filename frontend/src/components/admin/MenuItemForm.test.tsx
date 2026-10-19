import { describe, it, expect, afterEach, vi } from 'vitest'
import { render, screen, cleanup } from '@testing-library/react'
import { MenuItemForm } from './MenuItemForm'

vi.mock('../../api/menuApi', () => ({
  createMenuItem: vi.fn(),
  updateMenuItem: vi.fn(),
  uploadMenuItemImage: vi.fn(),
}))

describe('MenuItemForm', () => {
  afterEach(() => {
    cleanup()
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
})
