using DoctorRegistry.Application.Practitioners;
using DoctorRegistry.Domain.Practitioners;

namespace DoctorRegistry.Application.Abstractions.Persistence;

public interface IApplicationDbContext
{
    Task<Practitioner?> FindPractitionerAsync(Guid id, bool tracking, CancellationToken cancellationToken = default);

    Task<PagedResult<Practitioner>> SearchPractitionersAsync(SearchPractitionersQuery query,
        CancellationToken cancellationToken = default);

    void AddPractitioner(Practitioner practitioner);

    Task<int> TryRegisterProcessedMessageAsync(Guid messageId, DateTimeOffset processedAt,
        CancellationToken cancellationToken = default);

    Task<IApplicationDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
