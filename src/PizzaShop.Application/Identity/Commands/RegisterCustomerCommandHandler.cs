using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Identity.Abstractions;
using PizzaShop.Application.Identity.Dtos;
using PizzaShop.Domain.Customers;
using PizzaShop.Domain.Loyalty;

namespace PizzaShop.Application.Identity.Commands;

/// <summary>
/// Handles <see cref="RegisterCustomerCommand"/> (api-layer.md 2.4-2.6, ADR-0026). Creates the
/// three linked pieces of a customer's identity/profile and persists them in one
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call (same scoped <c>DbContext</c> behind all
/// three repositories): <see cref="UserAccount"/> (role Customer), then <see cref="Customer"/>,
/// then <see cref="LoyaltyAccount"/>. The Customer &lt;-&gt; LoyaltyAccount link is
/// one-directional — only <see cref="LoyaltyAccount.CustomerId"/> carries the reference
/// (ADR-0029) — so <see cref="Customer"/> is created first, and its generated id feeds
/// <see cref="LoyaltyAccount.Create(Guid)"/>; no shared, pre-generated id is needed.
/// </summary>
public sealed class RegisterCustomerCommandHandler : ICommandHandler<RegisterCustomerCommand, AuthResultDto>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILoyaltyAccountRepository _loyaltyAccountRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RegisterCustomerCommandHandler(
        IUserAccountRepository userAccountRepository,
        ICustomerRepository customerRepository,
        ILoyaltyAccountRepository loyaltyAccountRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _userAccountRepository = userAccountRepository;
        _customerRepository = customerRepository;
        _loyaltyAccountRepository = loyaltyAccountRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<AuthResultDto> Handle(RegisterCustomerCommand command, CancellationToken cancellationToken)
    {
        var email = UserAccount.NormalizeEmail(command.Email);

        // Primary guard, defense-in-depth alongside the unique index on Email
        // (api-layer.md 2.6, ADR-0026) — the index is the final backstop against a race, this
        // check gives the common case a clean 409 instead of a raw constraint violation.
        if (await _userAccountRepository.ExistsByEmailAsync(email, cancellationToken))
            throw new ConflictException($"An account with email '{email}' already exists.");

        var now = _clock.UtcNow;
        var passwordHash = _passwordHasher.Hash(command.Password);
        var userAccount = UserAccount.Create(email, passwordHash, UserRole.Customer, now);

        var customer = Customer.Create(
            userAccount.Id,
            command.FullName,
            email,
            now,
            command.PhoneNumber);
        var loyaltyAccount = LoyaltyAccount.Create(customer.Id);

        await _userAccountRepository.AddAsync(userAccount, cancellationToken);
        await _loyaltyAccountRepository.AddAsync(loyaltyAccount, cancellationToken);
        await _customerRepository.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var token = _jwtTokenGenerator.Generate(userAccount, customer.Id);
        return new AuthResultDto(token, userAccount.Id, userAccount.Role, customer.Id);
    }
}
