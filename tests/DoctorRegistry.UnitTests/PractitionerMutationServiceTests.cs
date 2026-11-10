using DoctorRegistry.Application.Practitioners;
using Xunit;

namespace DoctorRegistry.UnitTests;

public sealed class PractitionerMutationServiceTests
{
    [Fact]
    public void Creates_and_updates_practitioner_through_domain_methods()
    {
        var service = new PractitionerMutationService();
        var created = service.Create(Guid.NewGuid(), Command("Ada"), DateTimeOffset.UtcNow);
        var version = created.Version;

        service.Update(created,
            new UpdatePractitionerCommand("Grace", "Hopper", "+375291234567", "Neurologist", "Clinic",
                "grace@example.test", false, version), DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.Equal("Grace", created.FirstName);
        Assert.False(created.IsActive);
        Assert.NotEqual(version, created.Version);
    }

    private static CreatePractitionerCommand Command(string firstName) => new(firstName, "Lovelace", "+375291234567",
        "Therapist", "Clinic", "ada@example.test", true);
}
