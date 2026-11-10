using DoctorRegistry.Application.Imports;
using DoctorRegistry.Application.Practitioners;
using FluentValidation;
using FluentValidation.Results;

namespace DoctorRegistry.Api.Endpoints;

public static class PractitionerImportEndpoints
{
    public static IEndpointRouteBuilder MapPractitionerImportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/practitioners/import", Submit)
            .WithName("SubmitPractitionerImport")
            .WithSummary("Submit a Practitioner for asynchronous import")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        return app;
    }

    public static async Task<IResult> Submit(
        PractitionerImportMessageV1 request,
        IPractitionerImportPublisher publisher,
        IValidator<CreatePractitionerCommand> validator,
        CancellationToken cancellationToken)
    {
        if (request.PractitionerId == Guid.Empty)
            throw new ValidationException([new ValidationFailure(nameof(request.PractitionerId), "PractitionerId is required.")]);

        if (request.SchemaVersion is not (0 or PractitionerImportMessageV1.CurrentSchemaVersion))
            throw new ValidationException([
                new ValidationFailure(nameof(request.SchemaVersion),
                    $"Only schema version {PractitionerImportMessageV1.CurrentSchemaVersion} is supported.")
            ]);

        var command = new CreatePractitionerCommand(request.FirstName, request.LastName, request.Phone,
            request.Speciality, request.PlaceOfWork, request.Email, request.IsActive);
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var message = request with
        {
            MessageId = request.MessageId == Guid.Empty ? Guid.NewGuid() : request.MessageId,
            SchemaVersion = PractitionerImportMessageV1.CurrentSchemaVersion
        };
        await publisher.PublishAsync(message, cancellationToken);

        return Results.Accepted(value: new { messageId = message.MessageId });
    }
}
