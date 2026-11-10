using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using DoctorRegistry.Infrastructure.Persistence;
using DoctorRegistry.Application.Abstractions.Persistence;
using DoctorRegistry.Application.Imports;
using DoctorRegistry.Infrastructure.Messaging;

namespace DoctorRegistry.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DoctorRegistry");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'DoctorRegistry' is required.");
        services.AddDbContext<DoctorRegistryDbContext>(o => o.UseNpgsql(connectionString));
        services.AddScoped<IApplicationDbContext>(p => p.GetRequiredService<DoctorRegistryDbContext>());
        var rabbitMq = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>() ??
                       new RabbitMqOptions();
        rabbitMq.Validate();
        services.AddSingleton(rabbitMq);
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<RabbitMqTopology>();
        services.AddSingleton<IPractitionerImportPublisher, RabbitMqPractitionerImportPublisher>();
        return services;
    }
}