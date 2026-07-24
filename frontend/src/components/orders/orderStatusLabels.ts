import type { OrderStatus } from '../../api/types'

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
