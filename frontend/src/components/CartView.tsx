import { Link } from 'react-router-dom'
import { useCart } from '../hooks/useCart'

export function CartView() {
  const { items, totalAmount, setQuantity, removeItem, clear } = useCart()

  if (items.length === 0) {
    return <p className="empty-state">Koszyk jest pusty.</p>
  }

  const currency = items[0]?.currency ?? 'PLN'

  return (
    <div>
      {items.map((item) => (
        <div className="cart-item" key={item.key}>
          <div className="cart-item__details">
            <div>
              {item.menuItemName}
              {item.variantName ? ` — ${item.variantName}` : ''}
            </div>
            {item.extraNames.length > 0 && <div className="cart-item__meta">Dodatki: {item.extraNames.join(', ')}</div>}
            <div className="cart-item__meta">
              {item.unitPriceAmount.toFixed(2)} {item.currency} / szt.
            </div>
          </div>
          <input
            type="number"
            min={1}
            max={100}
            className="cart-item__quantity"
            value={item.quantity}
            onChange={(e) => setQuantity(item.key, Number(e.target.value))}
          />
          <button type="button" onClick={() => removeItem(item.key)}>
            Usuń
          </button>
        </div>
      ))}

      <div className="cart-summary">
        <span>Suma (orientacyjnie)</span>
        <span>
          {totalAmount.toFixed(2)} {currency}
        </span>
      </div>

      <div className="cart-actions">
        {/* Sits right next to "Przejdź do kasy" and emptied the whole cart -- every item with its
            variant and extras -- on one click, so a near-miss meant rebuilding the order. */}
        <button
          type="button"
          onClick={() => {
            if (window.confirm('Usunąć wszystkie pozycje z koszyka?')) clear()
          }}
        >
          Wyczyść koszyk
        </button>
        <Link to="/checkout" className="add-to-cart-btn">
          Przejdź do kasy
        </Link>
      </div>
    </div>
  )
}
