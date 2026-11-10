using DoctorRegistry.Application.Extensions;
using DoctorRegistry.Infrastructure.Extensions;
using DoctorRegistry.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using DoctorRegistry.Worker;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<PractitionerImportConsumer>();

var host = builder.Build();

await using (var scope = host.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DoctorRegistryDbContext>();
    await db.Database.MigrateAsync();
}

await host.RunAsync();
