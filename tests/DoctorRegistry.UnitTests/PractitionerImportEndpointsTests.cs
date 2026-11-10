using DoctorRegistry.Api.Endpoints;
using DoctorRegistry.Application.Imports;
using DoctorRegistry.Application.Practitioners;
using FluentValidation;
using Xunit;

namespace DoctorRegistry.UnitTests;

public sealed class PractitionerImportEndpointsTests
{
    [Fact]
    public async Task Submit_publishes_import_and_generates_message_id()
    {
        var publisher = new RecordingPublisher();
        var request = new PractitionerImportMessageV1(Guid.Empty, 0, Guid.NewGuid(), "Ada", "Lovelace",
            "+375291234567", "Therapist", "Clinic", "ada@example.test", true);

        await PractitionerImportEndpoints.Submit(request, publisher, new CreatePractitionerCommandValidator(), default);

        Assert.NotNull(publisher.Message);
        Assert.NotEqual(Guid.Empty, publisher.Message!.MessageId);
        Assert.Equal(PractitionerImportMessageV1.CurrentSchemaVersion, publisher.Message.SchemaVersion);
        Assert.Equal(request.PractitionerId, publisher.Message.PractitionerId);
    }

    [Fact]
    public async Task Submit_rejects_invalid_import_payload()
    {
        var publisher = new RecordingPublisher();
        var request = new PractitionerImportMessageV1(Guid.NewGuid(), 0, Guid.NewGuid(), "", "Lovelace",
            "+375291234567", "Therapist", "Clinic", "not-an-email", true);

        await Assert.ThrowsAsync<ValidationException>(() =>
            PractitionerImportEndpoints.Submit(request, publisher, new CreatePractitionerCommandValidator(), default));
        Assert.Null(publisher.Message);
    }

    private sealed class RecordingPublisher : IPractitionerImportPublisher
    {
        public PractitionerImportMessageV1? Message { get; private set; }

        public Task PublishAsync(PractitionerImportMessageV1 message, CancellationToken cancellationToken = default)
        {
            Message = message;
            return Task.CompletedTask;
        }
    }
}
