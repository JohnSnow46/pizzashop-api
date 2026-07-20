using PizzaShop.Application.Common.Abstractions;

namespace PizzaShop.Api.Tests.TestSupport;

/// <summary>
/// No-op <see cref="IUnitOfWork"/> for tests — the in-memory repositories commit immediately
/// on <c>AddAsync</c>/<c>UpdateAsync</c>, so there is nothing left for <c>SaveChangesAsync</c>
/// to flush.
/// </summary>
public sealed class NoopUnitOfWork : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
