using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRfidGatewayAndBoarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "api_key",
                table: "rfid_devices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mac_address",
                table: "rfid_devices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "boarding_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rfid_card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_stop_id = table.Column<Guid>(type: "uuid", nullable: true),
                    bus_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rfid_device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scan_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    scanned_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    is_manual_override = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_boarding_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_boarding_events_buses_bus_id",
                        column: x => x.bus_id,
                        principalTable: "buses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_boarding_events_rfid_cards_rfid_card_id",
                        column: x => x.rfid_card_id,
                        principalTable: "rfid_cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_boarding_events_rfid_devices_rfid_device_id",
                        column: x => x.rfid_device_id,
                        principalTable: "rfid_devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_boarding_events_route_stops_route_stop_id",
                        column: x => x.route_stop_id,
                        principalTable: "route_stops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_boarding_events_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_boarding_events_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "unknown_card_taps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bus_id = table.Column<Guid>(type: "uuid", nullable: true),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rfid_device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    device_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    card_uid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    scanned_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unknown_card_taps", x => x.id);
                    table.ForeignKey(
                        name: "FK_unknown_card_taps_buses_bus_id",
                        column: x => x.bus_id,
                        principalTable: "buses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_unknown_card_taps_rfid_devices_rfid_device_id",
                        column: x => x.rfid_device_id,
                        principalTable: "rfid_devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_unknown_card_taps_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_boarding_events_bus_id",
                table: "boarding_events",
                column: "bus_id");

            migrationBuilder.CreateIndex(
                name: "IX_boarding_events_rfid_card_id",
                table: "boarding_events",
                column: "rfid_card_id");

            migrationBuilder.CreateIndex(
                name: "IX_boarding_events_rfid_device_id",
                table: "boarding_events",
                column: "rfid_device_id");

            migrationBuilder.CreateIndex(
                name: "IX_boarding_events_route_stop_id",
                table: "boarding_events",
                column: "route_stop_id");

            migrationBuilder.CreateIndex(
                name: "IX_boarding_events_school_id_scanned_at",
                table: "boarding_events",
                columns: new[] { "school_id", "scanned_at" });

            migrationBuilder.CreateIndex(
                name: "IX_boarding_events_student_id_scanned_at",
                table: "boarding_events",
                columns: new[] { "student_id", "scanned_at" });

            migrationBuilder.CreateIndex(
                name: "IX_boarding_events_trip_id_student_id_scanned_at",
                table: "boarding_events",
                columns: new[] { "trip_id", "student_id", "scanned_at" });

            migrationBuilder.CreateIndex(
                name: "IX_unknown_card_taps_bus_id",
                table: "unknown_card_taps",
                column: "bus_id");

            migrationBuilder.CreateIndex(
                name: "IX_unknown_card_taps_card_uid",
                table: "unknown_card_taps",
                column: "card_uid");

            migrationBuilder.CreateIndex(
                name: "IX_unknown_card_taps_rfid_device_id",
                table: "unknown_card_taps",
                column: "rfid_device_id");

            migrationBuilder.CreateIndex(
                name: "IX_unknown_card_taps_school_id_scanned_at",
                table: "unknown_card_taps",
                columns: new[] { "school_id", "scanned_at" });

            migrationBuilder.CreateIndex(
                name: "IX_unknown_card_taps_trip_id",
                table: "unknown_card_taps",
                column: "trip_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "boarding_events");

            migrationBuilder.DropTable(
                name: "unknown_card_taps");

            migrationBuilder.DropColumn(
                name: "api_key",
                table: "rfid_devices");

            migrationBuilder.DropColumn(
                name: "mac_address",
                table: "rfid_devices");
        }
    }
}
