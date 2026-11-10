using DoctorRegistry.Domain.Practitioners;
using Xunit;

namespace DoctorRegistry.UnitTests;

public sealed class PractitionerTests
{
    [Fact]
    public void Update_changes_version_and_preserves_identity()
    {
        var practitioner = Practitioner.Create("Ada", "Lovelace", "+375291234567", "Therapist", "Hospital",
            "ada@example.com", true, DateTimeOffset.UtcNow);
        var id = practitioner.Id;
        var version = practitioner.Version;

        practitioner.Update("Ada", "Byron", "+375291234567", "Therapist", "Hospital", "ada@example.com", false,
            DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.Equal(id, practitioner.Id);
        Assert.NotEqual(version, practitioner.Version);
        Assert.False(practitioner.IsActive);
    }

    [Fact]
    public void Deleted_practitioner_cannot_be_updated()
    {
        var practitioner = Practitioner.Create("Ada", "Lovelace", "+375291234567", "Therapist", "Hospital",
            "ada@example.com", true, DateTimeOffset.UtcNow);
        practitioner.Delete(DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => practitioner.Activate(DateTimeOffset.UtcNow));
    }
}