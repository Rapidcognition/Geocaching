using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Geocaching.Migrations
{
    public partial class Something : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_FoundGeocache",
                table: "FoundGeocache");

            migrationBuilder.AddColumn<int>(
                name: "FoundGeocacheId",
                table: "FoundGeocache",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_FoundGeocache",
                table: "FoundGeocache",
                column: "FoundGeocacheId");

            migrationBuilder.CreateIndex(
                name: "IX_FoundGeocache_PersonId",
                table: "FoundGeocache",
                column: "PersonId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_FoundGeocache",
                table: "FoundGeocache");

            migrationBuilder.DropIndex(
                name: "IX_FoundGeocache_PersonId",
                table: "FoundGeocache");

            migrationBuilder.DropColumn(
                name: "FoundGeocacheId",
                table: "FoundGeocache");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FoundGeocache",
                table: "FoundGeocache",
                columns: new[] { "PersonId", "GeocacheId" });
        }
    }
}
