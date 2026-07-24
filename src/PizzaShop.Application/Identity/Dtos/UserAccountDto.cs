using PizzaShop.Application.Common.Abstractions;

namespace PizzaShop.Application.Identity.Dtos;

/// <summary>
/// Read-model of a <see cref="UserAccount"/> for admin screens — never includes
/// <see cref="UserAccount.PasswordHash"/>.
/// </summary>
public sealed record UserAccountDto(Guid Id, string Email, UserRole Role, bool IsActive, DateTimeOffset CreatedAt);
