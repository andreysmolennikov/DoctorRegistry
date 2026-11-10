using DoctorRegistry.Api.Fhir;
using DoctorRegistry.Application.Practitioners;
using FluentValidation;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;

namespace DoctorRegistry.Api.Endpoints;

public static class FhirPractitionerEndpoints
{
    private const string FhirJson = "application/fhir+json";

    public static IEndpointRouteBuilder MapFhirPractitionerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/fhir/Practitioner");
        group.MapGet("/{id:guid}", Get);
        group.MapGet("", Search);
        group.MapPut("/{id:guid}", Update);
        return app;
    }

    private static async Task<IResult> Get(Guid id, GetPractitionerByIdHandler handler, HttpResponse response,
        CancellationToken ct)
    {
        try
        {
            var practitioner = await handler.Handle(id, ct);
            SetVersionHeaders(response, practitioner);
            return FhirOk(practitioner.ToFhir());
        }
        catch (Exception ex)
        {
            return Outcome(ex);
        }
    }

    private static async Task<IResult> Search(HttpRequest request, SearchPractitionersHandler handler,
        CancellationToken ct)
    {
        try
        {
            var q = request.Query;
            var count = int.TryParse(q["_count"], out var parsedCount) ? parsedCount : 20;
            var page = int.TryParse(q["page"], out var parsedPage) ? parsedPage : 1;
            var result =
                await handler.Handle(
                    new SearchPractitionersQuery(ParseBool(q["active"]), q["speciality"], q["place-of-work"], null,
                        q["phone"], q["name"], q["family"], q["given"], page, count), ct);
            var baseUrl = request.GetDisplayUrl();
            var links = new List<FhirBundleLink> { new("self", baseUrl) };
            if (result.Page * result.PageSize < result.TotalCount)
                links.Add(new("next", WithPage(request, result.Page + 1)));
            if (result.Page > 1) links.Add(new("previous", WithPage(request, result.Page - 1)));
            var bundle = new FhirBundle("Bundle", "searchset", result.TotalCount, links,
                result.Items.Select(x =>
                        new FhirBundleEntry($"{request.Scheme}://{request.Host}/fhir/Practitioner/{x.Id}", x.ToFhir()))
                    .ToList());
            return FhirOk(bundle);
        }
        catch (Exception ex)
        {
            return Outcome(ex);
        }
    }

    private static async Task<IResult> Update(Guid id, FhirPractitioner resource, HttpRequest request,
        HttpResponse response, UpdatePractitionerHandler handler, CancellationToken ct)
    {
        if (!string.Equals(resource.ResourceType, "Practitioner", StringComparison.Ordinal))
            return Outcome("invalid", "resourceType must be Practitioner.", StatusCodes.Status400BadRequest);
        if (!Guid.TryParse(resource.Id, out var bodyId) || bodyId != id)
            return Outcome("invalid", "Resource id must match the URL id.", StatusCodes.Status400BadRequest);
        if (!TryVersion(request.Headers.IfMatch, out var version))
            return Outcome("required", "If-Match with a current ETag is required.",
                StatusCodes.Status428PreconditionRequired);
        try
        {
            var updated = await handler.Handle(id, resource.ToUpdateCommand(version), ct);
            SetVersionHeaders(response, updated);
            return FhirOk(updated.ToFhir());
        }
        catch (Exception ex)
        {
            return Outcome(ex);
        }
    }

    private static bool? ParseBool(string? value) => bool.TryParse(value, out var parsed) ? parsed : null;

    private static string WithPage(HttpRequest request, int page)
    {
        var parameters = request.Query.Where(x => x.Key != "page")
            .ToDictionary(x => x.Key, x => (string?)x.Value.ToString());
        parameters["page"] = page.ToString();
        return $"{request.Scheme}://{request.Host}{QueryHelpers.AddQueryString(request.Path, parameters)}";
    }

    private static bool TryVersion(string? value, out Guid version) =>
        Guid.TryParse(value?.Trim().Trim('"'), out version);

    private static void SetVersionHeaders(HttpResponse response, PractitionerDto practitioner)
    {
        response.Headers.ETag = $"\"{practitioner.Version}\"";
        response.Headers.LastModified = practitioner.UpdatedAt.ToUniversalTime().ToString("R");
    }

    private static IResult FhirOk<T>(T resource) => Results.Json(resource, contentType: FhirJson);

    private static IResult Outcome(Exception ex) => ex switch
    {
        NotFoundException => Outcome("not-found", ex.Message, StatusCodes.Status404NotFound),
        ConcurrencyConflictException => Outcome("conflict", ex.Message, StatusCodes.Status412PreconditionFailed),
        ValidationException validation => Results.Json(
            new FhirOperationOutcome("OperationOutcome",
                validation.Errors.Select(e => new FhirIssue("error", "invalid", e.ErrorMessage, [e.PropertyName]))
                    .ToList()), statusCode: StatusCodes.Status400BadRequest, contentType: FhirJson),
        _ => Outcome("exception", "The FHIR request could not be processed.", StatusCodes.Status500InternalServerError)
    };

    private static IResult Outcome(string code, string diagnostics, int status) => Results.Json(
        new FhirOperationOutcome("OperationOutcome", [new("error", code, diagnostics)]), statusCode: status,
        contentType: FhirJson);
}
