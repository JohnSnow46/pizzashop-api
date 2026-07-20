using Microsoft.EntityFrameworkCore;
using Npgsql;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;

namespace PizzaShop.Infrastructure.Persistence;

/// <summary>
/// Thin wrapper around <see cref="PizzaShopDbContext.SaveChangesAsync"/> — the transactional
/// boundary shared with the repositories, which never commit on their own
/// (infrastructure-layer.md 4.2).
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly PizzaShopDbContext _context;

    public UnitOfWork(PizzaShopDbContext context)
    {
        _context = context;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Final backstop against a registration race (api-layer.md 2.6): two concurrent
            // registrations with the same email can both pass ExistsByEmailAsync before either
            // commits; the unique index is what actually prevents the duplicate. Map that
            // low-level constraint violation onto the same ConflictException (409) the handler
            // already throws for the common case, instead of letting a raw
            // DbUpdateException/PostgresException surface as an unhandled 500.
            throw new ConflictException("The request conflicts with an existing record (a unique value is already in use).");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
