using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoutesAndTripEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "routes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    direction_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_routes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "route_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bus_id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    effective_from = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    effective_to = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_route_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_route_assignments_buses_bus_id",
                        column: x => x.bus_id,
                        principalTable: "buses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_route_assignments_drivers_driver_id",
                        column: x => x.driver_id,
                        principalTable: "drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_route_assignments_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "route_stops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stop_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sequence_order = table.Column<int>(type: "integer", nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: false),
                    longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: false),
                    scheduled_offset_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    geofence_radius_meters = table.Column<int>(type: "integer", nullable: false, defaultValue: 50),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_route_stops", x => x.id);
                    table.ForeignKey(
                        name: "FK_route_stops_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trips",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bus_id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_date = table.Column<DateOnly>(type: "date", nullable: false),
                    shift = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Scheduled"),
                    scheduled_start_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    actual_start_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    actual_end_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trips", x => x.id);
                    table.ForeignKey(
                        name: "FK_trips_buses_bus_id",
                        column: x => x.bus_id,
                        principalTable: "buses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trips_drivers_driver_id",
                        column: x => x.driver_id,
                        principalTable: "drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trips_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "trip_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    to_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trip_status_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_trip_status_history_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trip_stops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_stop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence_order = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    estimated_arrival_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    actual_arrival_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    actual_departure_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trip_stops", x => x.id);
                    table.ForeignKey(
                        name: "FK_trip_stops_route_stops_route_stop_id",
                        column: x => x.route_stop_id,
                        principalTable: "route_stops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trip_stops_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_route_assignments_bus_id",
                table: "route_assignments",
                column: "bus_id");

            migrationBuilder.CreateIndex(
                name: "IX_route_assignments_driver_id",
                table: "route_assignments",
                column: "driver_id");

            migrationBuilder.CreateIndex(
                name: "IX_route_assignments_route_id",
                table: "route_assignments",
                column: "route_id");

            migrationBuilder.CreateIndex(
                name: "ix_route_stops_route_sequence",
                table: "route_stops",
                columns: new[] { "route_id", "sequence_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_routes_school_code",
                table: "routes",
                columns: new[] { "school_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trip_status_history_trip_changed",
                table: "trip_status_history",
                columns: new[] { "trip_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_trip_stops_route_stop_id",
                table: "trip_stops",
                column: "route_stop_id");

            migrationBuilder.CreateIndex(
                name: "ix_trip_stops_trip_sequence",
                table: "trip_stops",
                columns: new[] { "trip_id", "sequence_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trips_bus_date_shift",
                table: "trips",
                columns: new[] { "bus_id", "trip_date", "shift" });

            migrationBuilder.CreateIndex(
                name: "ix_trips_driver_date_shift",
                table: "trips",
                columns: new[] { "driver_id", "trip_date", "shift" });

            migrationBuilder.CreateIndex(
                name: "IX_trips_route_id",
                table: "trips",
                column: "route_id");

            migrationBuilder.CreateIndex(
                name: "ix_trips_school_date_shift",
                table: "trips",
                columns: new[] { "school_id", "trip_date", "shift" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "route_assignments");

            migrationBuilder.DropTable(
                name: "trip_status_history");

            migrationBuilder.DropTable(
                name: "trip_stops");

            migrationBuilder.DropTable(
                name: "route_stops");

            migrationBuilder.DropTable(
                name: "trips");

            migrationBuilder.DropTable(
                name: "routes");
        }
    }
}
