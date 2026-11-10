using DoctorRegistry.Api.Fhir;
using DoctorRegistry.Application.Practitioners;
using Xunit;

namespace DoctorRegistry.UnitTests;

public sealed class FhirPractitionerMapperTests
{
    [Fact]
    public void Maps_application_dto_to_fhir_practitioner()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var dto = new PractitionerDto(id, "Ada", "Lovelace", "+375291234567", "Therapist", "Clinic", "ada@example.test",
            true, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch,
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        var result = dto.ToFhir();

        Assert.Equal("Practitioner", result.ResourceType);
        Assert.Equal(id.ToString(), result.Id);
        Assert.Equal(dto.Version.ToString(), result.Meta!.VersionId);
        Assert.Equal("Lovelace", result.Name![0].Family);
        Assert.Equal("Clinic", result.Extension![0].ValueString);
    }
}
