using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Geocaching.Migrations
{
    public partial class First : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Person",
                columns: table => new
                {
                    PersonId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    FirstName = table.Column<string>(type: "varchar(50)", nullable: true),
                    LastName = table.Column<string>(type: "varchar(50)", nullable: true),
                    Latitude = table.Column<double>(nullable: false),
                    Longitude = table.Column<double>(nullable: false),
                    Country = table.Column<string>(type: "varchar(50)", nullable: true),
                    City = table.Column<string>(type: "varchar(50)", nullable: true),
                    StreetName = table.Column<string>(type: "varchar(50)", nullable: true),
                    StreetNumber = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Person", x => x.PersonId);
                });

            migrationBuilder.CreateTable(
                name: "Geocaches",
                columns: table => new
                {
                    GeocacheId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Latitude = table.Column<double>(nullable: false),
                    Longitude = table.Column<double>(nullable: false),
                    Content = table.Column<string>(type: "varchar(255)", nullable: true),
                    Message = table.Column<string>(type: "varchar(255)", nullable: true),
                    PersonId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Geocaches", x => x.GeocacheId);
                    table.ForeignKey(
                        name: "FK_Geocaches_Person_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Person",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FoundGeocaches",
                columns: table => new
                {
                    PersonId = table.Column<int>(nullable: false),
                    GeocacheId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoundGeocaches", x => new { x.PersonId, x.GeocacheId });
                    table.ForeignKey(
                        name: "FK_FoundGeocaches_Geocaches_GeocacheId",
                        column: x => x.GeocacheId,
                        principalTable: "Geocaches",
                        principalColumn: "GeocacheId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FoundGeocaches_Person_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Person",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FoundGeocaches_GeocacheId",
                table: "FoundGeocaches",
                column: "GeocacheId");

            migrationBuilder.CreateIndex(
                name: "IX_Geocaches_PersonId",
                table: "Geocaches",
                column: "PersonId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FoundGeocaches");

            migrationBuilder.DropTable(
                name: "Geocaches");

            migrationBuilder.DropTable(
                name: "Person");
        }
    }
}
