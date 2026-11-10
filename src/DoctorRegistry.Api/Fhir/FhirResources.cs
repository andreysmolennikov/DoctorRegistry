using System.Text.Json.Serialization;
using DoctorRegistry.Application.Practitioners;

namespace DoctorRegistry.Api.Fhir;

public sealed class FhirPractitioner
{
    [JsonPropertyName("resourceType")] public string ResourceType { get; init; } = "Practitioner";
    [JsonPropertyName("id")] public string? Id { get; init; }
    [JsonPropertyName("meta")] public FhirMeta? Meta { get; init; }
    [JsonPropertyName("identifier")] public IReadOnlyList<FhirIdentifier>? Identifier { get; init; }
    [JsonPropertyName("active")] public bool? Active { get; init; }
    [JsonPropertyName("name")] public IReadOnlyList<FhirHumanName>? Name { get; init; }
    [JsonPropertyName("telecom")] public IReadOnlyList<FhirContactPoint>? Telecom { get; init; }
    [JsonPropertyName("qualification")] public IReadOnlyList<FhirQualification>? Qualification { get; init; }
    [JsonPropertyName("extension")] public IReadOnlyList<FhirExtension>? Extension { get; init; }
}

public sealed record FhirMeta(
    [property: JsonPropertyName("versionId")]
    string? VersionId,
    [property: JsonPropertyName("lastUpdated")]
    DateTimeOffset? LastUpdated);

public sealed record FhirIdentifier(
    [property: JsonPropertyName("system")] string? System,
    [property: JsonPropertyName("value")] string? Value);

public sealed record FhirHumanName(
    [property: JsonPropertyName("family")] string? Family,
    [property: JsonPropertyName("given")] IReadOnlyList<string>? Given);

public sealed record FhirContactPoint(
    [property: JsonPropertyName("system")] string? System,
    [property: JsonPropertyName("value")] string? Value);

public sealed record FhirQualification([property: JsonPropertyName("code")] FhirCodeableConcept? Code);

public sealed record FhirCodeableConcept([property: JsonPropertyName("text")] string? Text);

public sealed record FhirExtension(
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("valueString")]
    string? ValueString);

public sealed record FhirBundle(
    [property: JsonPropertyName("resourceType")]
    string ResourceType,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("link")] IReadOnlyList<FhirBundleLink> Link,
    [property: JsonPropertyName("entry")] IReadOnlyList<FhirBundleEntry> Entry);

public sealed record FhirBundleLink(
    [property: JsonPropertyName("relation")]
    string Relation,
    [property: JsonPropertyName("url")] string Url);

public sealed record FhirBundleEntry(
    [property: JsonPropertyName("fullUrl")]
    string FullUrl,
    [property: JsonPropertyName("resource")]
    FhirPractitioner Resource);

public sealed record FhirOperationOutcome(
    [property: JsonPropertyName("resourceType")]
    string ResourceType,
    [property: JsonPropertyName("issue")] IReadOnlyList<FhirIssue> Issue);

public sealed record FhirIssue(
    [property: JsonPropertyName("severity")]
    string Severity,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("diagnostics")]
    string Diagnostics,
    [property: JsonPropertyName("expression")]
    IReadOnlyList<string>? Expression = null);

public static class FhirPractitionerMapper
{
    public const string PlaceOfWorkUrl = "https://doctorregistry.example/fhir/StructureDefinition/place-of-work";

    public static FhirPractitioner ToFhir(this PractitionerDto x) => new()
    {
        Id = x.Id.ToString(),
        Meta = new(x.Version.ToString(), x.UpdatedAt),
        Active = x.IsActive,
        Identifier = [new("https://doctorregistry.example/practitioners", x.Id.ToString())],
        Name = [new(x.LastName, [x.FirstName])],
        Telecom = [new("phone", x.Phone), new("email", x.Email)],
        Qualification = [new(new(x.Speciality))],
        Extension = [new(PlaceOfWorkUrl, x.PlaceOfWork)]
    };

    public static UpdatePractitionerCommand ToUpdateCommand(this FhirPractitioner x, Guid version)
    {
        var name = x.Name?.FirstOrDefault();
        var first = name?.Given?.FirstOrDefault();
        var phone = x.Telecom?.FirstOrDefault(t => t.System == "phone")?.Value;
        var email = x.Telecom?.FirstOrDefault(t => t.System == "email")?.Value;
        var speciality = x.Qualification?.FirstOrDefault()?.Code?.Text;
        var work = x.Extension?.FirstOrDefault(e => e.Url == PlaceOfWorkUrl)?.ValueString;
        return new(first ?? "", name?.Family ?? "", phone ?? "", speciality ?? "", work ?? "", email ?? "",
            x.Active ?? true, version);
    }
}
