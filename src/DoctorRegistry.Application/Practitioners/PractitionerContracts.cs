namespace DoctorRegistry.Application.Practitioners;

public sealed record PractitionerDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Phone,
    string Speciality,
    string PlaceOfWork,
    string Email,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    Guid Version);

public sealed record CreatePractitionerCommand(
    string FirstName,
    string LastName,
    string Phone,
    string Speciality,
    string PlaceOfWork,
    string Email,
    bool IsActive = true);

public sealed record UpdatePractitionerCommand(
    string FirstName,
    string LastName,
    string Phone,
    string Speciality,
    string PlaceOfWork,
    string Email,
    bool IsActive,
    Guid Version);

public sealed record SearchPractitionersQuery(
    bool? IsActive,
    string? Speciality,
    string? PlaceOfWork,
    string? Email,
    string? Phone,
    string? Name,
    string? Family = null,
    string? Given = null,
    int Page = 1,
    int PageSize = 20);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed class NotFoundException(string message) : Exception(message);

public sealed class ConcurrencyConflictException(string message) : Exception(message);