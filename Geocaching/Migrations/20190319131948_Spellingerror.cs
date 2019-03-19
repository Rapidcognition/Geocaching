using Microsoft.EntityFrameworkCore.Migrations;

namespace Geocaching.Migrations
{
    public partial class Spellingerror : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FoundGeocaches_Geocaches_GeocacheId",
                table: "FoundGeocaches");

            migrationBuilder.DropForeignKey(
                name: "FK_FoundGeocaches_Person_PersonId",
                table: "FoundGeocaches");

            migrationBuilder.DropForeignKey(
                name: "FK_Geocaches_Person_PersonId",
                table: "Geocaches");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Geocaches",
                table: "Geocaches");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FoundGeocaches",
                table: "FoundGeocaches");

            migrationBuilder.RenameTable(
                name: "Geocaches",
                newName: "Geocache");

            migrationBuilder.RenameTable(
                name: "FoundGeocaches",
                newName: "FoundGeocache");

            migrationBuilder.RenameIndex(
                name: "IX_Geocaches_PersonId",
                table: "Geocache",
                newName: "IX_Geocache_PersonId");

            migrationBuilder.RenameIndex(
                name: "IX_FoundGeocaches_GeocacheId",
                table: "FoundGeocache",
                newName: "IX_FoundGeocache_GeocacheId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Geocache",
                table: "Geocache",
                column: "GeocacheId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FoundGeocache",
                table: "FoundGeocache",
                columns: new[] { "PersonId", "GeocacheId" });

            migrationBuilder.AddForeignKey(
                name: "FK_FoundGeocache_Geocache_GeocacheId",
                table: "FoundGeocache",
                column: "GeocacheId",
                principalTable: "Geocache",
                principalColumn: "GeocacheId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FoundGeocache_Person_PersonId",
                table: "FoundGeocache",
                column: "PersonId",
                principalTable: "Person",
                principalColumn: "PersonId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Geocache_Person_PersonId",
                table: "Geocache",
                column: "PersonId",
                principalTable: "Person",
                principalColumn: "PersonId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FoundGeocache_Geocache_GeocacheId",
                table: "FoundGeocache");

            migrationBuilder.DropForeignKey(
                name: "FK_FoundGeocache_Person_PersonId",
                table: "FoundGeocache");

            migrationBuilder.DropForeignKey(
                name: "FK_Geocache_Person_PersonId",
                table: "Geocache");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Geocache",
                table: "Geocache");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FoundGeocache",
                table: "FoundGeocache");

            migrationBuilder.RenameTable(
                name: "Geocache",
                newName: "Geocaches");

            migrationBuilder.RenameTable(
                name: "FoundGeocache",
                newName: "FoundGeocaches");

            migrationBuilder.RenameIndex(
                name: "IX_Geocache_PersonId",
                table: "Geocaches",
                newName: "IX_Geocaches_PersonId");

            migrationBuilder.RenameIndex(
                name: "IX_FoundGeocache_GeocacheId",
                table: "FoundGeocaches",
                newName: "IX_FoundGeocaches_GeocacheId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Geocaches",
                table: "Geocaches",
                column: "GeocacheId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FoundGeocaches",
                table: "FoundGeocaches",
                columns: new[] { "PersonId", "GeocacheId" });

            migrationBuilder.AddForeignKey(
                name: "FK_FoundGeocaches_Geocaches_GeocacheId",
                table: "FoundGeocaches",
                column: "GeocacheId",
                principalTable: "Geocaches",
                principalColumn: "GeocacheId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FoundGeocaches_Person_PersonId",
                table: "FoundGeocaches",
                column: "PersonId",
                principalTable: "Person",
                principalColumn: "PersonId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Geocaches_Person_PersonId",
                table: "Geocaches",
                column: "PersonId",
                principalTable: "Person",
                principalColumn: "PersonId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
