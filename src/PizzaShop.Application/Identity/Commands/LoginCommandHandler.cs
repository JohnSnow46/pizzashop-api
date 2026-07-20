using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Identity.Abstractions;
using PizzaShop.Application.Identity.Dtos;

namespace PizzaShop.Application.Identity.Commands;

/// <summary>
/// Handles <see cref="LoginCommand"/> (api-layer.md 2.4/2.7, ADR-0026). Unknown email, wrong
/// password, and a deactivated account are all reported as the same
/// <see cref="InvalidCredentialsException"/> — the response never reveals which check failed.
/// Only a <see cref="UserRole.Customer"/> account carries a <c>CustomerId</c> claim
/// (api-layer.md 2.5); staff accounts have none.
/// </summary>
public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, AuthResultDto>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IUserAccountRepository userAccountRepository,
        ICustomerRepository customerRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userAccountRepository = userAccountRepository;
        _customerRepository = customerRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResultDto> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var email = UserAccount.NormalizeEmail(command.Email);
        var account = await _userAccountRepository.GetByEmailAsync(email, cancellationToken);

        if (account is null || !account.IsActive || !_passwordHasher.Verify(command.Password, account.PasswordHash))
            throw new InvalidCredentialsException();

        Guid? customerId = null;
        if (account.Role == UserRole.Customer)
        {
            var customer = await _customerRepository.GetByUserAccountIdAsync(account.Id, cancellationToken);
            customerId = customer?.Id;
        }

        var token = _jwtTokenGenerator.Generate(account, customerId);
        return new AuthResultDto(token, account.Id, account.Role, customerId);
    }
}
