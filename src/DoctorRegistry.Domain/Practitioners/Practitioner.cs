namespace DoctorRegistry.Domain.Practitioners;

public sealed class Practitioner
{
    private Practitioner()
    {
    }

    private Practitioner(Guid id, string firstName, string lastName, string phone, string speciality,
        string placeOfWork, string email, bool isActive, DateTimeOffset now)
    {
        Id = id;
        IsActive = isActive;
        CreatedAt = now;
        UpdatedAt = now;
        Version = Guid.NewGuid();

        SetDetails(firstName, lastName, phone, speciality, placeOfWork, email);
    }

    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Phone { get; private set; } = null!;
    public string Speciality { get; private set; } = null!;
    public string PlaceOfWork { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid Version { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public static Practitioner Create(string firstName, string lastName, string phone, string speciality,
        string placeOfWork, string email, bool isActive, DateTimeOffset now) => new(Guid.NewGuid(), firstName, lastName,
        phone, speciality, placeOfWork, email, isActive, now);

    public static Practitioner Create(Guid id, string firstName, string lastName, string phone, string speciality,
        string placeOfWork, string email, bool isActive, DateTimeOffset now)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        return new(id, firstName, lastName, phone, speciality, placeOfWork, email, isActive, now);
    }

    public void Update(string firstName, string lastName, string phone, string speciality, string placeOfWork,
        string email, bool isActive, DateTimeOffset now)
    {
        EnsureNotDeleted();
        SetDetails(firstName, lastName, phone, speciality, placeOfWork, email);
        IsActive = isActive;
        Touch(now);
    }

    public void Activate(DateTimeOffset now)
    {
        EnsureNotDeleted();
        if (!IsActive)
        {
            IsActive = true;
            Touch(now);
        }
    }

    public void Deactivate(DateTimeOffset now)
    {
        EnsureNotDeleted();
        if (IsActive)
        {
            IsActive = false;
            Touch(now);
        }
    }

    public void Delete(DateTimeOffset now)
    {
        EnsureNotDeleted();
        DeletedAt = now;
        Touch(now);
    }

    private void SetDetails(string firstName, string lastName, string phone, string speciality, string placeOfWork,
        string email)
    {
        FirstName = Required(firstName, nameof(firstName), 100);
        LastName = Required(lastName, nameof(lastName), 100);
        Phone = Required(phone, nameof(phone), 32);
        Speciality = Required(speciality, nameof(speciality), 150);
        PlaceOfWork = Required(placeOfWork, nameof(placeOfWork), 200);
        Email = Required(email, nameof(email), 320);
    }

    private void Touch(DateTimeOffset now)
    {
        UpdatedAt = now;
        Version = Guid.NewGuid();
    }

    private void EnsureNotDeleted()
    {
        if (DeletedAt is not null) throw new InvalidOperationException("A deleted practitioner cannot be modified.");
    }

    private static string Required(string value, string name, int maxLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) throw new ArgumentException("Value is required.", name);
        if (trimmed.Length > maxLength) throw new ArgumentOutOfRangeException(name, $"Maximum length is {maxLength}.");
        return trimmed;
    }
}
