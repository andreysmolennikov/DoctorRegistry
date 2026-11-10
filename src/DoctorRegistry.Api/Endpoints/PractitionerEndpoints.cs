using DoctorRegistry.Application.Practitioners;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace DoctorRegistry.Api.Endpoints;

public static class PractitionerEndpoints
{
    public static IEndpointRouteBuilder MapPractitionerEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/practitioners");
        g.MapPost("", Create);
        g.MapGet("/{id:guid}", Get);
        g.MapGet("", Search);
        g.MapPut("/{id:guid}", Update);
        g.MapDelete("/{id:guid}", Delete);
        return app;
    }

    private static async Task<IResult> Create(CreatePractitionerCommand x, CreatePractitionerHandler h,
        CancellationToken ct)
    {
        var result = await h.Handle(x, ct);
        return Results.Created($"/api/practitioners/{result.Id}", result);
    }

    private static async Task<IResult> Get(Guid id, GetPractitionerByIdHandler h, CancellationToken ct) =>
        Results.Ok(await h.Handle(id, ct));

    private static async Task<IResult> Search([AsParameters] SearchPractitionersQuery q, SearchPractitionersHandler h,
        CancellationToken ct) => Results.Ok(await h.Handle(q, ct));

    private static async Task<IResult> Update(Guid id, UpdatePractitionerCommand x, UpdatePractitionerHandler h,
        CancellationToken ct) => Results.Ok(await h.Handle(id, x, ct));

    private static async Task<IResult> Delete(Guid id, [FromQuery] Guid version, DeletePractitionerHandler h,
        CancellationToken ct)
    {
        await h.Handle(id, version, ct);
        return Results.NoContent();
    }

    public static IResult ToProblem(Exception ex) => ex switch
    {
        NotFoundException => Results.NotFound(new { error = ex.Message }),
        ConcurrencyConflictException => Results.Conflict(new { error = ex.Message }),
        ValidationException v => Results.ValidationProblem(v.Errors.GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),
        _ => Results.Problem()
    };
}
