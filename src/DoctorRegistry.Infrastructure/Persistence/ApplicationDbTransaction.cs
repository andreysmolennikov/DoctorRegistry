using DoctorRegistry.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace DoctorRegistry.Infrastructure.Persistence;

public sealed class ApplicationDbTransaction(IDbContextTransaction transaction) : IApplicationDbTransaction
{
    public Task CommitAsync(CancellationToken ct = default) => transaction.CommitAsync(ct);
    public Task RollbackAsync(CancellationToken ct = default) => transaction.RollbackAsync(ct);
    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}
