using DoctorRegistry.Application.Abstractions.Persistence;
using DoctorRegistry.Application.Practitioners;
using FluentValidation;

namespace DoctorRegistry.Application.Imports;

public sealed class ImportPractitionerHandler(
    IApplicationDbContext db,
    PractitionerMutationService mutations,
    IValidator<CreatePractitionerCommand> validator)
{
    public async Task<PractitionerImportResult> Handle(PractitionerImportMessageV1 message,
        CancellationToken cancellationToken)
    {
        if (message.SchemaVersion != PractitionerImportMessageV1.CurrentSchemaVersion ||
            message.MessageId == Guid.Empty ||
            message.PractitionerId == Guid.Empty) throw new ValidationException("Invalid practitioner import message.");

        var command = new CreatePractitionerCommand(message.FirstName, message.LastName, message.Phone,
            message.Speciality, message.PlaceOfWork, message.Email, message.IsActive);

        await validator.ValidateAndThrowAsync(command, cancellationToken);

        await using var transaction = await db.BeginTransactionAsync(cancellationToken);

        if (await db.TryRegisterProcessedMessageAsync(message.MessageId, DateTimeOffset.UtcNow, cancellationToken) == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return PractitionerImportResult.Duplicate;
        }

        var practitioner = await db.FindPractitionerAsync(message.PractitionerId, true, cancellationToken);

        if (practitioner is null)
            db.AddPractitioner(mutations.Create(message.PractitionerId, command, DateTimeOffset.UtcNow));
        else
            mutations.Update(practitioner,
                new UpdatePractitionerCommand(command.FirstName, command.LastName, command.Phone, command.Speciality,
                    command.PlaceOfWork, command.Email, command.IsActive, practitioner.Version), DateTimeOffset.UtcNow);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return PractitionerImportResult.Processed;
    }
}
