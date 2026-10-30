import { useEffect } from 'react'

const APP_NAME = 'PizzaShop'

/**
 * Sets the browser tab title to "<title> — PizzaShop" while the calling page is mounted and
 * restores the previous title on unmount. Every route used to share the static "PizzaShop" from
 * index.html, so the cart, checkout and an order being tracked in another tab all looked the same.
 */
export function useDocumentTitle(title: string): void {
  useEffect(() => {
    const previous = document.title
    document.title = `${title} — ${APP_NAME}`
    return () => {
      document.title = previous
    }
  }, [title])
}
