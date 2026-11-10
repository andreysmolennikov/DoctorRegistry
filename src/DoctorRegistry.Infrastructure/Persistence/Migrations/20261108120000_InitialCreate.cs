using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace DoctorRegistry.Infrastructure.Persistence.Migrations;

[DbContext(typeof(DoctorRegistryDbContext))]
[Migration("20261108120000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "practitioners",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                FirstName = table.Column<string>(maxLength: 100, nullable: false),
                LastName = table.Column<string>(maxLength: 100, nullable: false),
                Phone = table.Column<string>(maxLength: 32, nullable: false),
                Speciality = table.Column<string>(maxLength: 150, nullable: false),
                PlaceOfWork = table.Column<string>(maxLength: 200, nullable: false),
                Email = table.Column<string>(maxLength: 320, nullable: false),
                IsActive = table.Column<bool>(nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(nullable: false),
                Version = table.Column<Guid>(nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(nullable: true)
            }, constraints: table => table.PrimaryKey("PK_practitioners", x => x.Id));
        migrationBuilder.CreateIndex("IX_practitioners_LastName_FirstName_Id", "practitioners", new[] { "LastName", "FirstName", "Id" });
        migrationBuilder.CreateIndex("IX_practitioners_Email", "practitioners", "Email");
        migrationBuilder.CreateIndex("IX_practitioners_Phone", "practitioners", "Phone");
        migrationBuilder.CreateIndex("IX_practitioners_Speciality", "practitioners", "Speciality");
        migrationBuilder.CreateIndex("IX_practitioners_PlaceOfWork", "practitioners", "PlaceOfWork");
        migrationBuilder.CreateIndex("IX_practitioners_IsActive", "practitioners", "IsActive");
        migrationBuilder.Sql("CREATE INDEX \"IX_practitioners_search\" ON practitioners USING GIN (to_tsvector('simple', \"FirstName\" || ' ' || \"LastName\"));");
        migrationBuilder.Sql("INSERT INTO practitioners (\"Id\", \"FirstName\", \"LastName\", \"Phone\", \"Speciality\", \"PlaceOfWork\", \"Email\", \"IsActive\", \"CreatedAt\", \"UpdatedAt\", \"Version\") VALUES ('11111111-1111-1111-1111-111111111111', 'Alice', 'Smith', '+375291111111', 'Therapist', 'Central Clinic', 'alice.smith@example.test', TRUE, '2026-01-01T00:00:00+00:00', '2026-01-01T00:00:00+00:00', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');");
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("practitioners");
}
