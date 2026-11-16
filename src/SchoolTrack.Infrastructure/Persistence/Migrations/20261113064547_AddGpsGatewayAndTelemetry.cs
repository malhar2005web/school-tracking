using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGpsGatewayAndTelemetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "api_key",
                table: "gps_devices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "latest_bus_locations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bus_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: true),
                    latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: false),
                    longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: false),
                    speed_kmh = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    heading = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    altitude = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: true),
                    accuracy_meters = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    ignition_on = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_online = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_latest_bus_locations", x => x.id);
                    table.ForeignKey(
                        name: "FK_latest_bus_locations_buses_bus_id",
                        column: x => x.bus_id,
                        principalTable: "buses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_latest_bus_locations_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "location_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bus_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: true),
                    latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: false),
                    longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: false),
                    speed_kmh = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    heading = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    altitude = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: true),
                    accuracy_meters = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    ignition_on = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    raw_payload = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_location_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_location_logs_buses_bus_id",
                        column: x => x.bus_id,
                        principalTable: "buses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_location_logs_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_latest_bus_locations_bus_id",
                table: "latest_bus_locations",
                column: "bus_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_latest_bus_locations_school_id_is_online",
                table: "latest_bus_locations",
                columns: new[] { "school_id", "is_online" });

            migrationBuilder.CreateIndex(
                name: "IX_latest_bus_locations_trip_id",
                table: "latest_bus_locations",
                column: "trip_id");

            migrationBuilder.CreateIndex(
                name: "IX_location_logs_bus_id_recorded_at",
                table: "location_logs",
                columns: new[] { "bus_id", "recorded_at" });

            migrationBuilder.CreateIndex(
                name: "IX_location_logs_school_id_recorded_at",
                table: "location_logs",
                columns: new[] { "school_id", "recorded_at" });

            migrationBuilder.CreateIndex(
                name: "IX_location_logs_trip_id_recorded_at",
                table: "location_logs",
                columns: new[] { "trip_id", "recorded_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "latest_bus_locations");

            migrationBuilder.DropTable(
                name: "location_logs");

            migrationBuilder.DropColumn(
                name: "api_key",
                table: "gps_devices");
        }
    }
}
