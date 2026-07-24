import { useEffect, useRef, useState } from 'react'
import {
  acceptOrder,
  cancelOrder,
  completeOrder,
  markReady,
  rejectOrder,
  startDelivery,
  startPreparation,
} from '../../api/ordersApi'
import type { FulfillmentType, Order, OrderStatus } from '../../api/types'
import { useOrderTracking } from '../../hooks/useOrderTracking'
import { PAYMENT_STATUS_LABELS, STATUS_LABELS } from './orderStatusLabels'

const FULFILLMENT_LABELS: Record<FulfillmentType, string> = {
  Delivery: 'Dostawa',
  Pickup: 'Odbiór osobisty',
}

/** Statuses that still belong in the queue — anything else means the order left it. */
const ACTIVE_QUEUE_STATUSES: ReadonlySet<OrderStatus> = new Set([
  'PendingAcceptance',
  'Accepted',
  'InPreparation',
  'Ready',
  'OutForDelivery',
])

interface QueueAction {
  label: string
  run: () => Promise<void>
  variant?: 'danger'
  confirmMessage?: string
}

/** Cancel is available for any order past acceptance; ADR-0018 refunds it synchronously if paid online. */
function buildCancelAction(order: Order, orderId: string): QueueAction {
  const triggersRefund = order.paymentMethod === 'Online' && order.paymentStatus === 'Paid'
  return {
    label: 'Anuluj zamówienie',
    variant: 'danger',
    run: () => cancelOrder(orderId),
    confirmMessage: triggersRefund
      ? `Czy na pewno anulować zamówienie ${order.number}? Płatność zostanie automatycznie zwrócona.`
      : `Czy na pewno anulować zamówienie ${order.number}?`,
  }
}

function getActions(order: Order, orderId: string): QueueAction[] {
  switch (order.status) {
    case 'PendingAcceptance':
      return [
        { label: 'Przyjmij', run: () => acceptOrder(orderId) },
        { label: 'Odrzuć', run: () => rejectOrder(orderId), variant: 'danger' },
      ]
    case 'Accepted':
      return [{ label: 'Rozpocznij przygotowanie', run: () => startPreparation(orderId) }, buildCancelAction(order, orderId)]
    case 'InPreparation':
      return [{ label: 'Oznacz jako gotowe', run: () => markReady(orderId) }, buildCancelAction(order, orderId)]
    case 'Ready':
      return order.fulfillmentType === 'Delivery'
        ? [{ label: 'Wyślij do dostawy', run: () => startDelivery(orderId) }, buildCancelAction(order, orderId)]
        : [{ label: 'Zakończ', run: () => completeOrder(orderId) }, buildCancelAction(order, orderId)]
    case 'OutForDelivery':
      return [{ label: 'Zakończ', run: () => completeOrder(orderId) }, buildCancelAction(order, orderId)]
    default:
      return []
  }
}

interface EmployeeOrderRowProps {
  orderId: string
  onLeftQueue: (orderId: string) => void
}

/**
 * One queue row — live-tracked via the same `useOrderTracking` hook as the customer-facing
 * TrackOrderPage (ADR-0038), so an action taken by another staff member elsewhere updates this
 * row without a manual refresh.
 */
export function EmployeeOrderRow({ orderId, onLeftQueue }: EmployeeOrderRowProps) {
  const tracking = useOrderTracking({ orderId })
  const [pendingAction, setPendingAction] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const hasLeftRef = useRef(false)

  const status = tracking.order?.status

  useEffect(() => {
    if (!status || hasLeftRef.current) return
    if (!ACTIVE_QUEUE_STATUSES.has(status)) {
      hasLeftRef.current = true
      onLeftQueue(orderId)
    }
  }, [status, orderId, onLeftQueue])

  async function runAction(label: string, action: () => Promise<void>) {
    setPendingAction(label)
    setActionError(null)
    try {
      await action()
    } catch (err) {
      // ApiError extends Error, so this also surfaces its ProblemDetails-derived message.
      setActionError(err instanceof Error ? err.message : 'Nie udało się wykonać akcji.')
    } finally {
      setPendingAction(null)
    }
  }

  if (tracking.isLoading) {
    return (
      <li className="account-order-list__item">
        <p>Ładowanie...</p>
      </li>
    )
  }

  if (tracking.error || !tracking.order) {
    return (
      <li className="account-order-list__item">
        <p className="empty-state">Nie udało się pobrać zamówienia.</p>
      </li>
    )
  }

  const order = tracking.order
  const actions = getActions(order, orderId)

  return (
    <li className="account-order-list__item">
      <div className="account-order-list__details">
        <strong>{order.number}</strong>
        <span className="cart-item__meta">
          {STATUS_LABELS[order.status]} · {FULFILLMENT_LABELS[order.fulfillmentType]} · Płatność:{' '}
          {PAYMENT_STATUS_LABELS[order.paymentStatus]}
        </span>
        <span className="cart-item__meta">
          {order.contact.fullName} · {order.contact.phoneNumber}
        </span>
        <span className="cart-item__meta">
          {new Date(order.placedAt).toLocaleString('pl-PL')} · {order.items.length} poz. · {order.total.amount.toFixed(2)}{' '}
          {order.total.currency}
        </span>
        {actionError && <span className="checkout-error">{actionError}</span>}
        <div className="queue-actions">
          {actions.map((action) => (
            <button
              key={action.label}
              type="button"
              className={`queue-action-btn${action.variant === 'danger' ? ' queue-action-btn--danger' : ''}`}
              disabled={pendingAction !== null}
              onClick={() => {
                if (action.confirmMessage && !window.confirm(action.confirmMessage)) return
                void runAction(action.label, action.run)
              }}
            >
              {pendingAction === action.label ? 'Wysyłanie...' : action.label}
            </button>
          ))}
        </div>
      </div>
    </li>
  )
}
