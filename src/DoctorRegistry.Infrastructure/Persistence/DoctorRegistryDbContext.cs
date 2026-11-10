using DoctorRegistry.Application.Abstractions.Persistence;
using DoctorRegistry.Application.Practitioners;
using DoctorRegistry.Domain.Practitioners;
using DoctorRegistry.Infrastructure.Persistence.Messaging;
using Microsoft.EntityFrameworkCore;

namespace DoctorRegistry.Infrastructure.Persistence;

public sealed class DoctorRegistryDbContext(DbContextOptions<DoctorRegistryDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Practitioner> Practitioners => Set<Practitioner>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    public async Task<Practitioner?> FindPractitionerAsync(Guid id, bool tracking, CancellationToken ct = default) =>
        tracking
            ? await Practitioners.SingleOrDefaultAsync(x => x.Id == id, ct)
            : await Practitioners.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<PagedResult<Practitioner>> SearchPractitionersAsync(SearchPractitionersQuery q,
        CancellationToken ct = default)
    {
        IQueryable<Practitioner> x = Practitioners.AsNoTracking();
        if (q.IsActive is not null) x = x.Where(p => p.IsActive == q.IsActive);
        if (!string.IsNullOrWhiteSpace(q.Speciality)) x = x.Where(p => p.Speciality == q.Speciality.Trim());
        if (!string.IsNullOrWhiteSpace(q.PlaceOfWork)) x = x.Where(p => p.PlaceOfWork == q.PlaceOfWork.Trim());
        if (!string.IsNullOrWhiteSpace(q.Email)) x = x.Where(p => p.Email == q.Email.Trim());
        if (!string.IsNullOrWhiteSpace(q.Phone)) x = x.Where(p => p.Phone == q.Phone.Trim());
        if (!string.IsNullOrWhiteSpace(q.Name))
        {
            var name = $"%{q.Name.Trim()}%";
            x = x.Where(p => EF.Functions.ILike(p.FirstName, name) || EF.Functions.ILike(p.LastName, name));
        }

        if (!string.IsNullOrWhiteSpace(q.Family))
            x = x.Where(p => EF.Functions.ILike(p.LastName, $"%{q.Family.Trim()}%"));

        if (!string.IsNullOrWhiteSpace(q.Given))
            x = x.Where(p => EF.Functions.ILike(p.FirstName, $"%{q.Given.Trim()}%"));

        var total = await x.CountAsync(ct);
        var items = await x.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ThenBy(p => p.Id)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToListAsync(ct);
        return new(items, q.Page, q.PageSize, total);
    }

    public void AddPractitioner(Practitioner practitioner) => Practitioners.Add(practitioner);

    public Task<int> TryRegisterProcessedMessageAsync(Guid id, DateTimeOffset at, CancellationToken ct = default) =>
        Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO messaging.processed_messages (\"MessageId\", \"ProcessedAt\") VALUES ({id}, {at}) ON CONFLICT (\"MessageId\") DO NOTHING",
            ct);

    public async Task<IApplicationDbTransaction> BeginTransactionAsync(CancellationToken ct = default) =>
        new ApplicationDbTransaction(await Database.BeginTransactionAsync(ct));

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.ApplyConfigurationsFromAssembly(typeof(DoctorRegistryDbContext).Assembly);
    }
}
