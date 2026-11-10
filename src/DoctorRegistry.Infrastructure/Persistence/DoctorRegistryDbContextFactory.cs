using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DoctorRegistry.Infrastructure.Persistence;

/// <summary>Creates the context for EF Core CLI commands without starting an application host.</summary>
public sealed class DoctorRegistryDbContextFactory : IDesignTimeDbContextFactory<DoctorRegistryDbContext>
{
    public DoctorRegistryDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DoctorRegistry")
                               ??
                               "Host=localhost;Port=5432;Database=doctor_registry;Username=postgres;Password=postgres";

        return new DoctorRegistryDbContext(
            new DbContextOptionsBuilder<DoctorRegistryDbContext>()
                .UseNpgsql(connectionString)
                .Options);
    }
}