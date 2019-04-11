using Microsoft.EntityFrameworkCore.Migrations;

namespace Geocaching.Migrations
{
    public partial class AlmostDone2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeoCoordinate_Latitude",
                table: "Person");

            migrationBuilder.DropColumn(
                name: "GeoCoordinate_Longitude",
                table: "Person");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Geocache");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Geocache");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "GeoCoordinate_Latitude",
                table: "Person",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GeoCoordinate_Longitude",
                table: "Person",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Geocache",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Geocache",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
