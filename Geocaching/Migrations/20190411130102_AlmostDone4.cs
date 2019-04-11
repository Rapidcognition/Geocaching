using Microsoft.EntityFrameworkCore.Migrations;

namespace Geocaching.Migrations
{
    public partial class AlmostDone4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeoCoordinate_Altitude",
                table: "Geocache");

            migrationBuilder.DropColumn(
                name: "GeoCoordinate_Course",
                table: "Geocache");

            migrationBuilder.DropColumn(
                name: "GeoCoordinate_HorizontalAccuracy",
                table: "Geocache");

            migrationBuilder.DropColumn(
                name: "GeoCoordinate_Speed",
                table: "Geocache");

            migrationBuilder.DropColumn(
                name: "GeoCoordinate_VerticalAccuracy",
                table: "Geocache");

            migrationBuilder.RenameColumn(
                name: "GeoCoordinate_Longitude",
                table: "Geocache",
                newName: "Longitude");

            migrationBuilder.RenameColumn(
                name: "GeoCoordinate_Latitude",
                table: "Geocache",
                newName: "Latitude");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Longitude",
                table: "Geocache",
                newName: "GeoCoordinate_Longitude");

            migrationBuilder.RenameColumn(
                name: "Latitude",
                table: "Geocache",
                newName: "GeoCoordinate_Latitude");

            migrationBuilder.AddColumn<double>(
                name: "GeoCoordinate_Altitude",
                table: "Geocache",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GeoCoordinate_Course",
                table: "Geocache",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GeoCoordinate_HorizontalAccuracy",
                table: "Geocache",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GeoCoordinate_Speed",
                table: "Geocache",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GeoCoordinate_VerticalAccuracy",
                table: "Geocache",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
