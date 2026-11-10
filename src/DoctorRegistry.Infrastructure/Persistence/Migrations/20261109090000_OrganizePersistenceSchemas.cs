using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace DoctorRegistry.Infrastructure.Persistence.Migrations;

[DbContext(typeof(DoctorRegistryDbContext))]
[Migration("20261109090000_OrganizePersistenceSchemas")]
public partial class OrganizePersistenceSchemas : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "practitioner");
        migrationBuilder.EnsureSchema(name: "messaging");

        migrationBuilder.RenameTable(
            name: "practitioners",
            newName: "practitioners",
            newSchema: "practitioner");

        migrationBuilder.CreateTable(
            name: "processed_messages",
            schema: "messaging",
            columns: table => new
            {
                MessageId = table.Column<Guid>(nullable: false),
                ProcessedAt = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_processed_messages", x => x.MessageId));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "processed_messages", schema: "messaging");
        migrationBuilder.RenameTable(
            name: "practitioners",
            schema: "practitioner",
            newName: "practitioners");
    }
}
