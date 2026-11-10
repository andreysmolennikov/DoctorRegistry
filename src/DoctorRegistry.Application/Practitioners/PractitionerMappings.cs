using DoctorRegistry.Domain.Practitioners;

namespace DoctorRegistry.Application.Practitioners;

internal static class PractitionerMappings
{
    public static PractitionerDto ToDto(this Practitioner x) => new(x.Id, x.FirstName, x.LastName, x.Phone,
        x.Speciality, x.PlaceOfWork, x.Email, x.IsActive, x.CreatedAt, x.UpdatedAt, x.Version);
}
