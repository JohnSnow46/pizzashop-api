import type { OrderStatus } from '../../api/types'
import type { OrderTrackingConnectionState, UseOrderTrackingResult } from '../../hooks/useOrderTracking'

/** Polish label per OrderStatus (all 8 values, src/PizzaShop.Domain/Enums/OrderStatus.cs). */
const STATUS_LABELS: Record<OrderStatus, string> = {
  PendingAcceptance: 'Oczekuje na potwierdzenie',
  Accepted: 'Przyjęte',
  InPreparation: 'W przygotowaniu',
  Ready: 'Gotowe do odbioru',
  OutForDelivery: 'W drodze',
  Completed: 'Zrealizowane',
  Rejected: 'Odrzucone',
  Cancelled: 'Anulowane',
}

/** Terminal negative statuses — shown visually/textually distinct from Completed. */
const NEGATIVE_STATUSES: ReadonlySet<OrderStatus> = new Set(['Rejected', 'Cancelled'])

const CONNECTION_LABELS: Record<OrderTrackingConnectionState, string> = {
  connecting: 'Łączenie…',
  connected: 'Połączono na żywo',
  reconnecting: 'Ponowne łączenie…',
  disconnected: 'Rozłączono — dane mogą być nieaktualne',
}

/**
 * Presentational status view for order live-tracking (ADR-0038, Decision 6). Takes exactly
 * `UseOrderTrackingResult` as props (doesn't call the hook itself) so it can be reused/tested
 * independently of the SignalR transport.
 */
export function OrderTrackingStatus({ order, isLoading, error, connectionState }: UseOrderTrackingResult) {
  if (isLoading) {
    return <p>Ładowanie statusu zamówienia...</p>
  }

  if (error || !order) {
    return <p className="empty-state">Nie udało się pobrać statusu zamówienia. Spróbuj odświeżyć stronę.</p>
  }

  const isNegative = NEGATIVE_STATUSES.has(order.status)

  return (
    <div className="checkout-step">
      <p>
        Status zamówienia:{' '}
        <strong className={isNegative ? 'checkout-error' : undefined}>{STATUS_LABELS[order.status]}</strong>
      </p>

      {order.estimatedReadyAt && (
        <p className="checkout-hint">
          Szacowany czas gotowości/dostawy: {new Date(order.estimatedReadyAt).toLocaleString('pl-PL')}
        </p>
      )}

      <p className="checkout-hint">{CONNECTION_LABELS[connectionState]}</p>
    </div>
  )
}
