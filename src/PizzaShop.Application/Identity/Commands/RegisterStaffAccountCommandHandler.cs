using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Identity.Abstractions;
using PizzaShop.Application.Identity.Dtos;

namespace PizzaShop.Application.Identity.Commands;

/// <summary>
/// Handles <see cref="RegisterStaffAccountCommand"/> (api-layer.md 2.4, ADR-0017/0026). The
/// endpoint's <c>[Authorize(Roles = AuthRoles.Admin)]</c> only guarantees the caller is at
/// least a <see cref="UserRole.RestaurantAdmin"/>; the finer "who may create which role" rule
/// is state/role-dependent (depends on the *target* role in the request body) and therefore
/// lives here, not in a static policy attribute (ADR-0017):
/// <see cref="UserRole.RestaurantAdmin"/> may only create <see cref="UserRole.Employee"/>;
/// <see cref="UserRole.SuperAdmin"/> may create any staff role.
/// </summary>
public sealed class RegisterStaffAccountCommandHandler : ICommandHandler<RegisterStaffAccountCommand, AuthResultDto>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public RegisterStaffAccountCommandHandler(
        IUserAccountRepository userAccountRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        _userAccountRepository = userAccountRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<AuthResultDto> Handle(RegisterStaffAccountCommand command, CancellationToken cancellationToken)
    {
        EnsureCallerCanCreate(command.Role);

        var email = UserAccount.NormalizeEmail(command.Email);
        if (await _userAccountRepository.ExistsByEmailAsync(email, cancellationToken))
            throw new ConflictException($"An account with email '{email}' already exists.");

        var passwordHash = _passwordHasher.Hash(command.Password);
        var userAccount = UserAccount.Create(email, passwordHash, command.Role, _clock.UtcNow);

        await _userAccountRepository.AddAsync(userAccount, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var token = _jwtTokenGenerator.Generate(userAccount, customerId: null);
        return new AuthResultDto(token, userAccount.Id, userAccount.Role, CustomerId: null);
    }

    private void EnsureCallerCanCreate(UserRole targetRole)
    {
        var allowed = _currentUser.Role switch
        {
            UserRole.SuperAdmin => true,
            UserRole.RestaurantAdmin => targetRole == UserRole.Employee,
            _ => false,
        };

        if (!allowed)
            throw new ForbiddenOperationException(
                $"Role '{_currentUser.Role}' is not allowed to create an account with role '{targetRole}'.");
    }
}
