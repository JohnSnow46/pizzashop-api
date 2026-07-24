import type { OrderStatus, PaymentStatus } from '../../api/types'

/** Polish label per OrderStatus (all 8 values, src/PizzaShop.Domain/Enums/OrderStatus.cs). */
export const STATUS_LABELS: Record<OrderStatus, string> = {
  PendingAcceptance: 'Oczekuje na potwierdzenie',
  Accepted: 'Przyjęte',
  InPreparation: 'W przygotowaniu',
  Ready: 'Gotowe do odbioru',
  OutForDelivery: 'W drodze',
  Completed: 'Zrealizowane',
  Rejected: 'Odrzucone',
  Cancelled: 'Anulowane',
}

/** Polish label per PaymentStatus (src/PizzaShop.Domain/Enums/PaymentStatus.cs). */
export const PAYMENT_STATUS_LABELS: Record<PaymentStatus, string> = {
  Pending: 'Oczekuje',
  Authorized: 'Autoryzowana',
  Paid: 'Opłacona',
  Refunded: 'Zwrócona',
  Failed: 'Nieudana',
}
