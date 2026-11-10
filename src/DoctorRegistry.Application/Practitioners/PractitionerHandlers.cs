using DoctorRegistry.Application.Abstractions.Persistence;
using FluentValidation;

namespace DoctorRegistry.Application.Practitioners;

public sealed class CreatePractitionerHandler(
    IApplicationDbContext db,
    IValidator<CreatePractitionerCommand> validator,
    PractitionerMutationService mutations)
{
    public async Task<PractitionerDto> Handle(CreatePractitionerCommand x, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(x, ct);
        var entity = mutations.Create(x, DateTimeOffset.UtcNow);
        db.AddPractitioner(entity);
        await db.SaveChangesAsync(ct);
        return entity.ToDto();
    }
}

public sealed class GetPractitionerByIdHandler(IApplicationDbContext db)
{
    public async Task<PractitionerDto> Handle(Guid id, CancellationToken ct) =>
        (await db.FindPractitionerAsync(id, false, ct))?.ToDto() ??
        throw new NotFoundException($"Practitioner '{id}' was not found.");
}

public sealed class SearchPractitionersHandler(IApplicationDbContext db, IValidator<SearchPractitionersQuery> validator)
{
    public async Task<PagedResult<PractitionerDto>> Handle(SearchPractitionersQuery q, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(q, ct);
        var result = await db.SearchPractitionersAsync(q, ct);
        return new(result.Items.Select(x => x.ToDto()).ToList(), result.Page, result.PageSize, result.TotalCount);
    }
}

public sealed class UpdatePractitionerHandler(
    IApplicationDbContext db,
    IValidator<UpdatePractitionerCommand> validator,
    PractitionerMutationService mutations)
{
    public async Task<PractitionerDto> Handle(Guid id, UpdatePractitionerCommand x, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(x, ct);
        var e = await db.FindPractitionerAsync(id, true, ct) ??
                throw new NotFoundException($"Practitioner '{id}' was not found.");
        if (e.Version != x.Version)
            throw new ConcurrencyConflictException("The practitioner has changed. Refresh and retry.");
        mutations.Update(e, x, DateTimeOffset.UtcNow);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            throw new ConcurrencyConflictException("The practitioner has changed. Refresh and retry.");
        }

        return e.ToDto();
    }
}

public sealed class DeletePractitionerHandler(IApplicationDbContext db)
{
    public async Task Handle(Guid id, Guid version, CancellationToken ct)
    {
        var e = await db.FindPractitionerAsync(id, true, ct) ??
                throw new NotFoundException($"Practitioner '{id}' was not found.");
        if (e.Version != version)
            throw new ConcurrencyConflictException("The practitioner has changed. Refresh and retry.");
        e.Delete(DateTimeOffset.UtcNow);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            throw new ConcurrencyConflictException("The practitioner has changed. Refresh and retry.");
        }
    }
}
