// Manual TypeScript mirror of PizzaShop.Application DTOs (ADR-0035, point 3; ADR-0036 extends
// it for the guest checkout iteration). We deliberately hand-write these instead of generating
// from OpenAPI. Keep this file in sync whenever the corresponding C# DTOs change:
//   - src/PizzaShop.Application/Catalog/Dtos/MenuItemDto.cs
//   - src/PizzaShop.Application/Catalog/Dtos/MenuItemVariantDto.cs
//   - src/PizzaShop.Application/Catalog/Dtos/IngredientDto.cs
//   - src/PizzaShop.Application/Common/Dtos/MoneyDto.cs
//   - src/PizzaShop.Application/Restaurant/Dtos/RestaurantConfigDto.cs (+ OpeningHoursDto)
//   - src/PizzaShop.Application/Orders/Commands/CreateOrderCommand.cs
//   - src/PizzaShop.Application/Orders/Dtos/ContactDetailsDto.cs
//   - src/PizzaShop.Application/Orders/Dtos/CreateOrderItemDto.cs
//   - src/PizzaShop.Application/Orders/Dtos/CreateOrderResultDto.cs
//   - src/PizzaShop.Application/Orders/Dtos/DeliveryAvailabilityDto.cs
//   - src/PizzaShop.Application/Promotions/Dtos/PromotionDiscountLineDto.cs
//   - src/PizzaShop.Application/Promotions/Dtos/PromotionDiscountPreviewDto.cs
//   - src/PizzaShop.Application/Promotions/Queries/ValidatePromotionCodeQuery.cs
//   - src/PizzaShop.Application/Orders/Dtos/OrderDto.cs (+ OrderItemDto, OrderItemExtraDto)
//   - src/PizzaShop.Domain/Enums/OrderStatus.cs
//   - src/PizzaShop.Domain/Enums/PaymentStatus.cs
//   - SignalROrderNotifier's "OrderStatusChanged" push payload (ADR-0038)
//   - src/PizzaShop.Application/Orders/Dtos/OrderSummaryDto.cs (ADR-0039)
//   - src/PizzaShop.Application/Loyalty/Dtos/LoyaltyBalanceDto.cs (ADR-0039)
//   - src/PizzaShop.Application/Loyalty/Dtos/LoyaltyTransactionDto.cs (ADR-0039)
//   - src/PizzaShop.Domain/Enums/LoyaltyTransactionType.cs (ADR-0039)
//   - src/PizzaShop.Application/Catalog/Commands/CreateMenuItemCommand.cs (admin catalog UI)
//   - src/PizzaShop.Application/Catalog/Commands/UpdateMenuItemCommand.cs (admin catalog UI, ADR-0016)
//   - src/PizzaShop.Application/Catalog/Commands/SetMenuItemAvailabilityCommand.cs (admin catalog UI)
//   - src/PizzaShop.Application/Catalog/Commands/CreateIngredientCommand.cs (admin catalog UI)
//   - src/PizzaShop.Application/Catalog/Commands/UpdateIngredientCommand.cs (admin catalog UI)
//   - src/PizzaShop.Application/Catalog/Dtos/MenuItemVariantDto.cs (MenuItemVariantInputDto, admin catalog UI)

/** Mirror of PizzaShop.Application.Common.Dtos.MoneyDto. */
export interface Money {
  amount: number
  currency: string
}

/**
 * Mirror of PizzaShop.Domain.Enums.MenuCategory. Program.cs registers a
 * JsonStringEnumConverter, so this arrives over the wire as a string, not a number.
 */
export type MenuCategory = 'Pizza' | 'Drink' | 'Side' | 'Dessert' | 'Sauce'

/** Mirror of PizzaShop.Application.Catalog.Dtos.IngredientDto. */
export interface Ingredient {
  id: string
  name: string
  extraPrice: Money
  isAvailable: boolean
  category: string | null
}

/** Mirror of PizzaShop.Application.Catalog.Dtos.MenuItemVariantDto. */
export interface MenuItemVariant {
  id: string
  name: string
  price: Money
  isDefault: boolean
}

/** Mirror of PizzaShop.Application.Catalog.Dtos.MenuItemDto. */
export interface MenuItem {
  id: string
  name: string
  description: string | null
  category: MenuCategory
  basePrice: Money
  isAvailable: boolean
  imageUrl: string | null
  variants: MenuItemVariant[]
  baseIngredients: Ingredient[]
  allowedExtras: Ingredient[]
}

/**
 * Mirror of PizzaShop.Application.Catalog.Dtos.MenuItemVariantInputDto (admin catalog UI).
 * `id: null` means "add new variant"; a set `id` means "existing variant, reconcile"
 * (UpdateMenuItemCommandHandler.ReconcileVariants, ADR-0016).
 */
export interface MenuItemVariantInput {
  id: string | null
  name: string
  price: Money
  isDefault: boolean
}

/** Mirror of PizzaShop.Application.Catalog.Commands.CreateMenuItemCommand (admin catalog UI). */
export interface CreateMenuItemCommand {
  name: string
  category: MenuCategory
  basePrice: Money
  description: string | null
  imageUrl: string | null
  baseIngredientIds: string[]
  allowedExtraIds: string[]
  variants: MenuItemVariantInput[]
}

/**
 * Mirror of PizzaShop.Application.Catalog.Commands.UpdateMenuItemCommand (admin catalog UI).
 * Full PUT/replace semantics for ingredients and variants — `id` is overridden by the route
 * on the server, matching docs/api-layer.md 1.1.
 */
export interface UpdateMenuItemCommand {
  id: string
  name: string
  description: string | null
  imageUrl: string | null
  basePrice: Money
  baseIngredientIds: string[]
  allowedExtraIds: string[]
  variants: MenuItemVariantInput[]
}

/**
 * Mirror of PizzaShop.Application.Catalog.Commands.SetMenuItemAvailabilityCommand (admin
 * catalog UI). `menuItemId` is overridden by the route on the server (docs/api-layer.md 1.1).
 */
export interface SetMenuItemAvailabilityCommand {
  menuItemId: string
  isAvailable: boolean
}

/** Mirror of PizzaShop.Application.Catalog.Commands.CreateIngredientCommand (admin catalog UI). */
export interface CreateIngredientCommand {
  name: string
  extraPrice: Money
  category: string | null
}

/**
 * Mirror of PizzaShop.Application.Catalog.Commands.UpdateIngredientCommand (admin catalog UI).
 * `id` is overridden by the route on the server (docs/api-layer.md 1.1).
 */
export interface UpdateIngredientCommand {
  id: string
  name: string
  extraPrice: Money
  isAvailable: boolean
}

/** Mirror of PizzaShop.Application.Restaurant.Dtos.TimeRangeDto. */
export interface TimeRange {
  start: string
  end: string
}

export type DayOfWeekName =
  | 'Monday'
  | 'Tuesday'
  | 'Wednesday'
  | 'Thursday'
  | 'Friday'
  | 'Saturday'
  | 'Sunday'

/**
 * Mirror of PizzaShop.Application.Restaurant.Dtos.OpeningHoursDto. Program.cs registers a
 * JsonStringEnumConverter globally, so System.Text.Json serializes the DayOfWeek-keyed
 * dictionary with the enum member names as JSON object keys (e.g. "Monday", "Sunday"), not
 * numbers.
 */
export interface OpeningHours {
  schedule: Record<DayOfWeekName, TimeRange[]>
}

/** Mirror of PizzaShop.Application.Common.Dtos.AddressDto. */
export interface Address {
  street: string
  buildingNumber: string
  city: string
  postalCode: string
  apartmentNumber: string | null
  notes: string | null
}

/** Mirror of PizzaShop.Application.Common.Dtos.GeoCoordinateDto. */
export interface GeoCoordinate {
  latitude: number
  longitude: number
}

/** Mirror of PizzaShop.Application.Restaurant.Dtos.RestaurantConfigDto. */
export interface RestaurantConfig {
  id: string
  name: string
  address: Address
  location: GeoCoordinate
  deliveryRadiusKm: number
  timeZoneId: string
  openingHours: OpeningHours
  contactPhone: string
  isAcceptingOrders: boolean
  minimumOrderValue: Money | null
  freeDeliveryThreshold: Money | null
  deliveryFee: Money
}

/**
 * Mirror of PizzaShop.Domain.Enums.FulfillmentType. Serialized as a string
 * (JsonStringEnumConverter, ADR-0035), not a number.
 */
export type FulfillmentType = 'Delivery' | 'Pickup'

/**
 * Mirror of PizzaShop.Domain.Enums.PaymentMethod. Serialized as a string
 * (JsonStringEnumConverter, ADR-0035), not a number.
 */
export type PaymentMethod = 'Online' | 'OnPickup'

/** Mirror of PizzaShop.Application.Orders.Dtos.ContactDetailsDto. */
export interface ContactDetails {
  fullName: string
  phoneNumber: string
  email: string | null
}

/** Mirror of PizzaShop.Application.Orders.Dtos.CreateOrderItemDto. */
export interface CreateOrderItem {
  menuItemId: string
  variantId: string | null
  quantity: number
  extraIngredientIds: string[]
  notes: string | null
}

/** Mirror of PizzaShop.Application.Orders.Commands.CreateOrderCommand. */
export interface CreateOrderCommand {
  contact: ContactDetails
  fulfillmentType: FulfillmentType
  deliveryAddress: Address | null
  items: CreateOrderItem[]
  requestedFulfillmentTime: string | null
  paymentMethod: PaymentMethod
  promotionCode: string | null
  /**
   * Points the customer chose to redeem at checkout (ADR-0040), or null for a guest/no
   * selection. The server recalculates and enforces the discount authoritatively — this
   * value is only a request.
   */
  pointsToRedeem: number | null
}

/** Mirror of PizzaShop.Application.Orders.Dtos.CreateOrderResultDto. */
export interface CreateOrderResult {
  orderId: string
  number: string
  guestTrackingToken: string | null
  paymentRedirectUrl: string | null
}

/** Mirror of PizzaShop.Application.Orders.Dtos.DeliveryAvailabilityDto. */
export interface DeliveryAvailability {
  isAvailable: boolean
  distanceKm: number | null
  deliveryFee: Money | null
}

/** Mirror of PizzaShop.Application.Promotions.Dtos.PromotionDiscountLineDto. */
export interface PromotionDiscountLine {
  menuItemId: string
  unitPrice: Money
  quantity: number
}

/** Mirror of PizzaShop.Application.Promotions.Queries.ValidatePromotionCodeQuery. */
export interface ValidatePromotionCodeRequest {
  code: string
  subtotal: Money
  deliveryFee: Money
  lines: PromotionDiscountLine[]
}

/** Mirror of PizzaShop.Application.Promotions.Dtos.PromotionDiscountPreviewDto. */
export interface PromotionDiscountPreview {
  isQualified: boolean
  discountAmount: Money | null
}

/**
 * Mirror of PizzaShop.Domain.Enums.OrderStatus. Serialized as a string
 * (JsonStringEnumConverter, ADR-0035), not a number.
 */
export type OrderStatus =
  | 'PendingAcceptance'
  | 'Accepted'
  | 'InPreparation'
  | 'Ready'
  | 'OutForDelivery'
  | 'Completed'
  | 'Rejected'
  | 'Cancelled'

/**
 * Mirror of PizzaShop.Domain.Enums.PaymentStatus. Serialized as a string
 * (JsonStringEnumConverter, ADR-0035), not a number.
 */
export type PaymentStatus = 'Pending' | 'Authorized' | 'Paid' | 'Refunded' | 'Failed'

/** Mirror of PizzaShop.Application.Orders.Dtos.OrderItemExtraDto. */
export interface OrderItemExtra {
  ingredientId: string
  name: string
  price: Money
}

/** Mirror of PizzaShop.Application.Orders.Dtos.OrderItemDto. */
export interface OrderItem {
  id: string
  menuItemId: string
  menuItemName: string
  variantId: string | null
  variantName: string | null
  unitPrice: Money
  quantity: number
  notes: string | null
  extras: OrderItemExtra[]
  lineTotal: Money
}

/** Mirror of PizzaShop.Application.Orders.Dtos.OrderDto. */
export interface Order {
  id: string
  number: string
  customerId: string | null
  contact: ContactDetails
  fulfillmentType: FulfillmentType
  deliveryAddress: Address | null
  placedAt: string
  requestedFulfillmentTime: string | null
  estimatedReadyAt: string | null
  status: OrderStatus
  paymentMethod: PaymentMethod
  paymentStatus: PaymentStatus
  subtotal: Money
  discountAmount: Money
  deliveryFee: Money
  total: Money
  items: OrderItem[]
}

/**
 * Mirror of the "OrderStatusChanged" SignalR push payload (SignalROrderNotifier,
 * ADR-0028/0038). Sent to clients subscribed via OrderTrackingHub.
 */
export interface OrderStatusChangedEvent {
  orderId: string
  status: OrderStatus
  estimatedReadyAt: string | null
}

/**
 * Mirror of PizzaShop.Application.Orders.Dtos.OrderSummaryDto (ADR-0039) — the row shape
 * for the logged-in customer's own order history (`GET /api/orders/mine`).
 */
export interface OrderSummary {
  id: string
  number: string
  placedAt: string
  status: OrderStatus
  fulfillmentType: FulfillmentType
  paymentStatus: PaymentStatus
  total: Money
  itemsCount: number
}

/**
 * Mirror of PizzaShop.Domain.Enums.LoyaltyTransactionType. Serialized as a string
 * (JsonStringEnumConverter, ADR-0035), not a number. 'Reversed' added by ADR-0040 — an
 * automatic refund of points when an order with redeemed points is cancelled/rejected.
 */
export type LoyaltyTransactionType = 'Earned' | 'Redeemed' | 'Adjusted' | 'Expired' | 'Reversed'

/** Mirror of PizzaShop.Application.Loyalty.Dtos.LoyaltyTransactionDto (ADR-0039). */
export interface LoyaltyTransaction {
  type: LoyaltyTransactionType
  points: number
  reason: string
  orderId: string | null
  occurredAt: string
}

/**
 * Mirror of PizzaShop.Application.Loyalty.Dtos.LoyaltyBalanceDto (`GET /api/loyalty/balance`).
 * `transactions` arrives sorted descending by `occurredAt` (ADR-0039).
 */
export interface LoyaltyBalance {
  pointsBalance: number
  transactions: LoyaltyTransaction[]
}
