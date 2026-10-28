import { describe, it, expect, afterEach, vi } from 'vitest'
import { render, screen, cleanup } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { Layout } from './Layout'

vi.mock('../hooks/useCart', () => ({
  useCart: () => ({ totalQuantity: 0 }),
}))

vi.mock('../hooks/useAuth', () => ({
  useAuth: () => ({
    isAuthenticated: true,
    user: { email: 'admin@pizza.test', role: 'RestaurantAdmin' },
    logout: vi.fn(),
  }),
}))

function renderAt(path: string) {
  render(
    <MemoryRouter initialEntries={[path]}>
      <Layout>
        <p>content</p>
      </Layout>
    </MemoryRouter>,
  )
}

describe('Layout', () => {
  afterEach(() => {
    cleanup()
  })

  it('marks the header link of the current page', () => {
    renderAt('/admin/promotions')

    expect(screen.getByRole('link', { name: 'Promocje' }).getAttribute('aria-current')).toBe('page')
    expect(screen.getByRole('link', { name: 'Raporty' }).getAttribute('aria-current')).toBeNull()
  })

  it('does not mark "Panel admina" on a more specific /admin page', () => {
    renderAt('/admin/promotions')
    expect(screen.getByRole('link', { name: 'Panel admina' }).getAttribute('aria-current')).toBeNull()

    cleanup()
    renderAt('/admin')
    expect(screen.getByRole('link', { name: 'Panel admina' }).getAttribute('aria-current')).toBe('page')
  })
})
