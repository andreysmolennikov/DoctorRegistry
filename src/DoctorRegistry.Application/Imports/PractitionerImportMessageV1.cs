namespace DoctorRegistry.Application.Imports;

public sealed record PractitionerImportMessageV1(
    Guid MessageId,
    int SchemaVersion,
    Guid PractitionerId,
    string FirstName,
    string LastName,
    string Phone,
    string Speciality,
    string PlaceOfWork,
    string Email,
    bool IsActive)
{
    public const int CurrentSchemaVersion = 1;
}

public enum PractitionerImportResult
{
    Processed,
    Duplicate
}
