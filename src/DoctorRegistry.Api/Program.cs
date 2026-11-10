using DoctorRegistry.Application.Extensions;
using DoctorRegistry.Infrastructure.Extensions;
using DoctorRegistry.Infrastructure.Persistence;
using DoctorRegistry.Api.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DoctorRegistryDbContext>();
    await db.Database.MigrateAsync();
}

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
    if (feature?.Error is not null) await PractitionerEndpoints.ToProblem(feature.Error).ExecuteAsync(context);
}));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "DoctorRegistry API"));
}

app.UseHttpsRedirection();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapPractitionerEndpoints();
app.MapPractitionerImportEndpoints();
app.MapFhirPractitionerEndpoints();

app.Run();

public partial class Program;
