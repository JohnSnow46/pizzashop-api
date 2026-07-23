// Manual TypeScript mirror of PizzaShop.Application.Identity DTOs/commands (ADR-0037). Keep in
// sync with:
//   - src/PizzaShop.Application/Identity/Dtos/AuthResultDto.cs
//   - src/PizzaShop.Application/Identity/Commands/LoginCommand.cs
//   - src/PizzaShop.Application/Identity/Commands/RegisterCustomerCommand.cs

/**
 * Mirror of PizzaShop.Domain.Enums.UserRole. Serialized as a string (JsonStringEnumConverter,
 * ADR-0035), not a number. `RegisterStaffAccountCommand` can produce non-Customer roles, but
 * this frontend iteration only ever logs in/registers customers.
 */
export type UserRole = 'Customer' | 'Employee' | 'RestaurantAdmin' | 'SuperAdmin'

/** Mirror of PizzaShop.Application.Identity.Dtos.AuthResultDto. */
export interface AuthResult {
  token: string
  userAccountId: string
  role: UserRole
  customerId: string | null
}

/** Mirror of PizzaShop.Application.Identity.Commands.LoginCommand. */
export interface LoginRequest {
  email: string
  password: string
}

/** Mirror of PizzaShop.Application.Identity.Commands.RegisterCustomerCommand. */
export interface RegisterRequest {
  email: string
  password: string
  fullName: string
  phoneNumber: string | null
}
