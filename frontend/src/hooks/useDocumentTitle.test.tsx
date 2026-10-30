import { describe, it, expect, beforeEach } from 'vitest'
import { renderHook } from '@testing-library/react'
import { useDocumentTitle } from './useDocumentTitle'

describe('useDocumentTitle', () => {
  beforeEach(() => {
    document.title = 'PizzaShop'
  })

  it('sets the page title with the app name', () => {
    renderHook(() => useDocumentTitle('Koszyk'))
    expect(document.title).toBe('Koszyk — PizzaShop')
  })

  it('follows a changed title', () => {
    const { rerender } = renderHook(({ title }) => useDocumentTitle(title), { initialProps: { title: 'Koszyk' } })
    rerender({ title: 'Zamówienie' })
    expect(document.title).toBe('Zamówienie — PizzaShop')
  })

  it('restores the previous title on unmount', () => {
    const { unmount } = renderHook(() => useDocumentTitle('Koszyk'))
    unmount()
    expect(document.title).toBe('PizzaShop')
  })
})
