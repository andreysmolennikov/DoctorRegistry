using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using DoctorRegistry.Application.Practitioners;
using DoctorRegistry.Application.Imports;

namespace DoctorRegistry.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreatePractitionerCommandValidator>();
        services.AddScoped<CreatePractitionerHandler>();
        services.AddScoped<PractitionerMutationService>();
        services.AddScoped<ImportPractitionerHandler>();
        services.AddScoped<GetPractitionerByIdHandler>();
        services.AddScoped<SearchPractitionersHandler>();
        services.AddScoped<UpdatePractitionerHandler>();
        services.AddScoped<DeletePractitionerHandler>();
        return services;
    }
}
