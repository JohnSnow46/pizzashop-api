import { describe, it, expect, afterEach, vi } from 'vitest'
import { render, screen, cleanup } from '@testing-library/react'
import { MenuItemVariantsEditor } from './MenuItemVariantsEditor'

describe('MenuItemVariantsEditor', () => {
  afterEach(cleanup)

  it('names each row’s fields and remove button', () => {
    render(
      <MenuItemVariantsEditor
        currency="PLN"
        onChange={vi.fn()}
        variants={[
          { id: 'v1', name: 'Mała', price: { amount: 25, currency: 'PLN' }, isDefault: true },
          { id: null, name: '', price: { amount: 0, currency: 'PLN' }, isDefault: false },
        ]}
      />,
    )

    expect(screen.getByRole('textbox', { name: 'Nazwa wariantu 1' })).toBeTruthy()
    expect(screen.getByRole('spinbutton', { name: 'Cena wariantu 2 (PLN)' })).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Usuń wariant Mała' })).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Usuń wariant 2' })).toBeTruthy()
  })
})
