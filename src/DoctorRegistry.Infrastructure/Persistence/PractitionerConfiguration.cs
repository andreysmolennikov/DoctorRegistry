using DoctorRegistry.Domain.Practitioners;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoctorRegistry.Infrastructure.Persistence;

public sealed class PractitionerConfiguration : IEntityTypeConfiguration<Practitioner>
{
    public void Configure(EntityTypeBuilder<Practitioner> b)
    {
        b.ToTable("practitioners", "practitioner");
        b.HasKey(x => x.Id);
        b.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        b.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(32).IsRequired();
        b.Property(x => x.Speciality).HasMaxLength(150).IsRequired();
        b.Property(x => x.PlaceOfWork).HasMaxLength(200).IsRequired();
        b.Property(x => x.Email).HasMaxLength(320).IsRequired();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasQueryFilter(x => x.DeletedAt == null);
        b.HasIndex(x => new { x.LastName, x.FirstName, x.Id });
        b.HasIndex(x => x.Email);
        b.HasIndex(x => x.Phone);
        b.HasIndex(x => x.Speciality);
        b.HasIndex(x => x.PlaceOfWork);
        b.HasIndex(x => x.IsActive);
    }
}
