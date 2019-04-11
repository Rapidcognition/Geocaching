using Microsoft.EntityFrameworkCore.Migrations;

namespace Geocaching.Migrations
{
    public partial class AlmostDone : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
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
                name: "GeoCoordinate_Latitude",
                table: "Geocache",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GeoCoordinate_Longitude",
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeoCoordinate_Latitude",
                table: "Person");

            migrationBuilder.DropColumn(
                name: "GeoCoordinate_Longitude",
                table: "Person");

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
                name: "GeoCoordinate_Latitude",
                table: "Geocache");

            migrationBuilder.DropColumn(
                name: "GeoCoordinate_Longitude",
                table: "Geocache");

            migrationBuilder.DropColumn(
                name: "GeoCoordinate_Speed",
                table: "Geocache");

            migrationBuilder.DropColumn(
                name: "GeoCoordinate_VerticalAccuracy",
                table: "Geocache");
        }
    }
}
