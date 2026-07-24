import { useEffect, useState } from 'react'
import { getMyOrders } from '../api/ordersApi'
import { getLoyaltyBalance } from '../api/loyaltyApi'
import { addAddress, getMyAddresses, removeAddress, setDefaultAddress } from '../api/customersApi'
import { ApiError } from '../api/client'
import type {
  Address,
  CustomerAddress,
  FulfillmentType,
  LoyaltyBalance,
  LoyaltyTransactionType,
  OrderSummary,
} from '../api/types'
import { PAYMENT_STATUS_LABELS, STATUS_LABELS } from '../components/orders/orderStatusLabels'
import { validateAddress } from '../checkout/validation'

const FULFILLMENT_LABELS: Record<FulfillmentType, string> = {
  Delivery: 'Dostawa',
  Pickup: 'Odbiór osobisty',
}

const LOYALTY_TYPE_LABELS: Record<LoyaltyTransactionType, string> = {
  Earned: 'Naliczono',
  Redeemed: 'Wykorzystano',
  Adjusted: 'Korekta',
  Expired: 'Wygasło',
  Reversed: 'Zwrot (anulowane zamówienie)',
}

function formatDateTime(value: string): string {
  return new Date(value).toLocaleString('pl-PL')
}

function formatMoney(amount: number, currency: string): string {
  return `${amount.toFixed(2)} ${currency}`
}

function formatAddress(address: Address): string {
  const apartment = address.apartmentNumber ? `/${address.apartmentNumber}` : ''
  return `${address.street} ${address.buildingNumber}${apartment}, ${address.postalCode} ${address.city}`
}

const EMPTY_NEW_ADDRESS: Address = {
  street: '',
  buildingNumber: '',
  city: '',
  postalCode: '',
  apartmentNumber: null,
  notes: null,
}

/**
 * `/account` (ADR-0039, RequireAuth-gated) — logged-in customer's order history + loyalty
 * points balance/history. Deliberately no link to a per-order detail view: the app has no
 * logged-in-customer order detail route yet (only the guest `/orders/track/:token` route and
 * the live-tracking view reachable right after checkout), so adding one is out of scope here.
 */
export function MyAccountPage() {
  const [orders, setOrders] = useState<OrderSummary[] | null>(null)
  const [loyalty, setLoyalty] = useState<LoyaltyBalance | null>(null)
  const [addresses, setAddresses] = useState<CustomerAddress[]>([])
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  const [newLabel, setNewLabel] = useState('')
  const [newAddress, setNewAddress] = useState<Address>(EMPTY_NEW_ADDRESS)
  const [addressErrors, setAddressErrors] = useState<Record<string, string>>({})
  const [addressApiError, setAddressApiError] = useState<string | null>(null)
  const [isSavingAddress, setIsSavingAddress] = useState(false)
  const [addressActionId, setAddressActionId] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    setIsLoading(true)
    setError(null)

    Promise.all([getMyOrders(), getLoyaltyBalance(), getMyAddresses()])
      .then(([myOrders, myLoyalty, myAddresses]) => {
        if (cancelled) return
        setOrders(myOrders)
        setLoyalty(myLoyalty)
        setAddresses(myAddresses)
        setIsLoading(false)
      })
      .catch((err: unknown) => {
        if (cancelled) return
        setError(err instanceof Error ? err.message : 'Nie udało się pobrać danych konta.')
        setIsLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [])

  async function handleAddAddress() {
    const validationErrors = validateAddress(newAddress)
    const errors: Record<string, string> = { ...validationErrors }
    if (newLabel.trim().length === 0) {
      errors.label = 'Podaj nazwę adresu.'
    }
    setAddressErrors(errors)
    if (Object.keys(errors).length > 0) {
      return
    }

    setIsSavingAddress(true)
    setAddressApiError(null)
    try {
      const created = await addAddress({ label: newLabel.trim(), address: newAddress, isDefault: addresses.length === 0 })
      setAddresses((prev) => (created.isDefault ? prev.map((a) => ({ ...a, isDefault: false })) : prev).concat(created))
      setNewLabel('')
      setNewAddress(EMPTY_NEW_ADDRESS)
      setAddressErrors({})
    } catch (err) {
      setAddressApiError(err instanceof ApiError ? (err.detail ?? err.message) : 'Nie udało się dodać adresu.')
    } finally {
      setIsSavingAddress(false)
    }
  }

  async function handleRemoveAddress(id: string) {
    setAddressActionId(id)
    setAddressApiError(null)
    try {
      await removeAddress(id)
      setAddresses((prev) => prev.filter((a) => a.id !== id))
    } catch (err) {
      setAddressApiError(err instanceof ApiError ? (err.detail ?? err.message) : 'Nie udało się usunąć adresu.')
    } finally {
      setAddressActionId(null)
    }
  }

  async function handleSetDefaultAddress(id: string) {
    setAddressActionId(id)
    setAddressApiError(null)
    try {
      await setDefaultAddress(id)
      setAddresses((prev) => prev.map((a) => ({ ...a, isDefault: a.id === id })))
    } catch (err) {
      setAddressApiError(err instanceof ApiError ? (err.detail ?? err.message) : 'Nie udało się ustawić adresu domyślnego.')
    } finally {
      setAddressActionId(null)
    }
  }

  if (isLoading) {
    return <p>Ładowanie konta...</p>
  }

  if (error) {
    return <p className="empty-state">{error}</p>
  }

  return (
    <div className="checkout-step">
      <h2>Moje konto</h2>

      <section>
        <h3>Historia zamówień</h3>
        {orders && orders.length > 0 ? (
          <ul className="account-order-list">
            {orders.map((order) => (
              <li key={order.id} className="account-order-list__item">
                <div className="account-order-list__details">
                  <strong>{order.number}</strong>
                  <span className="cart-item__meta">
                    {formatDateTime(order.placedAt)} · {FULFILLMENT_LABELS[order.fulfillmentType]} · {order.itemsCount}{' '}
                    poz.
                  </span>
                  <span className="cart-item__meta">
                    {STATUS_LABELS[order.status]} · Płatność: {PAYMENT_STATUS_LABELS[order.paymentStatus]}
                  </span>
                </div>
                <div>{formatMoney(order.total.amount, order.total.currency)}</div>
              </li>
            ))}
          </ul>
        ) : (
          <p className="empty-state">Nie masz jeszcze żadnych zamówień.</p>
        )}
      </section>

      <section>
        <h3>Punkty lojalnościowe</h3>
        <p>
          Saldo: <strong>{loyalty?.pointsBalance ?? 0} pkt</strong>
        </p>
        {loyalty && loyalty.transactions.length > 0 ? (
          <ul className="account-order-list">
            {loyalty.transactions.map((transaction, index) => (
              <li key={index} className="account-order-list__item">
                <div className="account-order-list__details">
                  <strong>{LOYALTY_TYPE_LABELS[transaction.type]}</strong>
                  <span className="cart-item__meta">{transaction.reason}</span>
                  <span className="cart-item__meta">{formatDateTime(transaction.occurredAt)}</span>
                </div>
                <div>{transaction.points > 0 ? `+${transaction.points}` : transaction.points} pkt</div>
              </li>
            ))}
          </ul>
        ) : (
          <p className="empty-state">Brak historii punktów.</p>
        )}
      </section>

      <section>
        <h3>Adresy dostawy</h3>

        {addressApiError && <p className="checkout-banner checkout-banner--error">{addressApiError}</p>}

        {addresses.length > 0 ? (
          <ul className="account-order-list">
            {addresses.map((entry) => (
              <li key={entry.id} className="account-order-list__item">
                <div className="account-order-list__details">
                  <strong>
                    {entry.label}
                    {entry.isDefault && <span className="menu-card__unavailable-badge"> domyślny</span>}
                  </strong>
                  <span className="cart-item__meta">{formatAddress(entry.address)}</span>
                </div>
                <div className="queue-actions">
                  {!entry.isDefault && (
                    <button
                      type="button"
                      className="queue-action-btn"
                      disabled={addressActionId === entry.id}
                      onClick={() => handleSetDefaultAddress(entry.id)}
                    >
                      Ustaw domyślny
                    </button>
                  )}
                  <button
                    type="button"
                    className="queue-action-btn queue-action-btn--danger"
                    disabled={addressActionId === entry.id}
                    onClick={() => handleRemoveAddress(entry.id)}
                  >
                    Usuń
                  </button>
                </div>
              </li>
            ))}
          </ul>
        ) : (
          <p className="empty-state">Nie masz jeszcze zapisanych adresów.</p>
        )}

        <div className="checkout-form-grid">
          <label className="checkout-field">
            Nazwa adresu (np. Dom, Praca)
            <input value={newLabel} onChange={(e) => setNewLabel(e.target.value)} />
            {addressErrors.label && <span className="checkout-error">{addressErrors.label}</span>}
          </label>

          <label className="checkout-field">
            Ulica
            <input
              value={newAddress.street}
              onChange={(e) => setNewAddress((prev) => ({ ...prev, street: e.target.value }))}
            />
            {addressErrors.street && <span className="checkout-error">{addressErrors.street}</span>}
          </label>

          <label className="checkout-field">
            Numer budynku
            <input
              value={newAddress.buildingNumber}
              onChange={(e) => setNewAddress((prev) => ({ ...prev, buildingNumber: e.target.value }))}
            />
            {addressErrors.buildingNumber && <span className="checkout-error">{addressErrors.buildingNumber}</span>}
          </label>

          <label className="checkout-field">
            Numer lokalu (opcjonalnie)
            <input
              value={newAddress.apartmentNumber ?? ''}
              onChange={(e) => setNewAddress((prev) => ({ ...prev, apartmentNumber: e.target.value || null }))}
            />
          </label>

          <label className="checkout-field">
            Miasto
            <input
              value={newAddress.city}
              onChange={(e) => setNewAddress((prev) => ({ ...prev, city: e.target.value }))}
            />
            {addressErrors.city && <span className="checkout-error">{addressErrors.city}</span>}
          </label>

          <label className="checkout-field">
            Kod pocztowy
            <input
              placeholder="00-001"
              value={newAddress.postalCode}
              onChange={(e) => setNewAddress((prev) => ({ ...prev, postalCode: e.target.value }))}
            />
            {addressErrors.postalCode && <span className="checkout-error">{addressErrors.postalCode}</span>}
          </label>
        </div>

        <div className="checkout-actions">
          <button type="button" className="add-to-cart-btn" disabled={isSavingAddress} onClick={handleAddAddress}>
            {isSavingAddress ? 'Zapisywanie...' : 'Dodaj adres'}
          </button>
        </div>
      </section>
    </div>
  )
}
