using DoctorRegistry.Domain.Practitioners;

namespace DoctorRegistry.Application.Practitioners;

public sealed class PractitionerMutationService
{
    public Practitioner Create(CreatePractitionerCommand command, DateTimeOffset now) => Practitioner.Create(
        command.FirstName, command.LastName, command.Phone, command.Speciality, command.PlaceOfWork, command.Email,
        command.IsActive, now);

    public Practitioner Create(Guid id, CreatePractitionerCommand command, DateTimeOffset now) => Practitioner.Create(
        id, command.FirstName, command.LastName, command.Phone, command.Speciality, command.PlaceOfWork, command.Email,
        command.IsActive, now);

    public void Update(Practitioner practitioner, UpdatePractitionerCommand command, DateTimeOffset now) =>
        practitioner.Update(command.FirstName, command.LastName, command.Phone, command.Speciality, command.PlaceOfWork,
            command.Email, command.IsActive, now);
}
