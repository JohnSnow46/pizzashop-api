import { useDocumentTitle } from '../hooks/useDocumentTitle'
import { CartView } from '../components/CartView'

export function CartPage() {
  useDocumentTitle('Koszyk')
  return (
    <div>
      <h2>Koszyk</h2>
      <CartView />
    </div>
  )
}
