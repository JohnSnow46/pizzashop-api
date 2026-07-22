using FluentAssertions;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Identity;
using PizzaShop.Infrastructure.Persistence;
using PizzaShop.Infrastructure.Tests.Fixtures;

namespace PizzaShop.Infrastructure.Tests.Persistence;

/// <summary>
/// Exercises the real unique-constraint-violation → <see cref="ConflictException"/> mapping in
/// <see cref="UnitOfWork"/> against a real Postgres instance (api-layer.md 2.6) — the handler
/// unit tests only mock <c>IUnitOfWork</c>, so they never touch <c>IsUniqueConstraintViolation</c>
/// or the Npgsql <c>SqlState</c> check itself.
/// </summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class UnitOfWorkTests : PostgresRepositoryTestBase
{
    public UnitOfWorkTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task SaveChangesAsync_DuplicateUserAccountEmail_ThrowsConflictException()
    {
        var now = DateTimeOffset.UtcNow;
        var email = $"race-{Guid.NewGuid():N}@example.com";

        await using (var seedContext = Fixture.CreateContext())
        {
            seedContext.UserAccounts.Add(UserAccount.Create(email, "hash-1", UserRole.Customer, now));
            await seedContext.SaveChangesAsync();
        }

        await using var context = Fixture.CreateContext();
        context.UserAccounts.Add(UserAccount.Create(email, "hash-2", UserRole.Customer, now));
        var unitOfWork = new UnitOfWork(context);

        var act = () => unitOfWork.SaveChangesAsync(CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
