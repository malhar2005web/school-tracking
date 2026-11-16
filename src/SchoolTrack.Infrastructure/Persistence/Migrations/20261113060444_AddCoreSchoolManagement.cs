using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCoreSchoolManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "buses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bus_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    registration_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    capacity = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    maintenance_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    insurance_expiry = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    fitness_expiry = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "classes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    grade = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    division = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    academic_year = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_classes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "drivers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    license_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    license_expiry = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    availability_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Available"),
                    years_experience = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    rating = table.Column<double>(type: "double precision", nullable: false, defaultValue: 5.0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_drivers", x => x.id);
                    table.ForeignKey(
                        name: "FK_drivers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "parents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    primary_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    alternate_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    emergency_contact_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    emergency_contact_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parents", x => x.id);
                    table.ForeignKey(
                        name: "FK_parents_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gps_devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    bus_id = table.Column<Guid>(type: "uuid", nullable: true),
                    imei = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sim_card_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    protocol_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Simulator"),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Offline"),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    last_location_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gps_devices", x => x.id);
                    table.ForeignKey(
                        name: "FK_gps_devices_buses_bus_id",
                        column: x => x.bus_id,
                        principalTable: "buses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "rfid_devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    bus_id = table.Column<Guid>(type: "uuid", nullable: true),
                    gate_location = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    port = table.Column<int>(type: "integer", nullable: true),
                    protocol_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Simulator"),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Offline"),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    last_scan_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rfid_devices", x => x.id);
                    table.ForeignKey(
                        name: "FK_rfid_devices_buses_bus_id",
                        column: x => x.bus_id,
                        principalTable: "buses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "students",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    photo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    date_of_birth = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    emergency_contact_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    seat_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    assigned_route_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_stop_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_students", x => x.id);
                    table.ForeignKey(
                        name: "FK_students_classes_class_id",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bus_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bus_id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_from = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    assigned_to = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    is_current = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bus_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_bus_assignments_buses_bus_id",
                        column: x => x.bus_id,
                        principalTable: "buses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bus_assignments_drivers_driver_id",
                        column: x => x.driver_id,
                        principalTable: "drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "parent_students",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relationship = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_primary_guardian = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    can_authorize_pickup = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parent_students", x => x.id);
                    table.ForeignKey(
                        name: "FK_parent_students_parents_parent_id",
                        column: x => x.parent_id,
                        principalTable: "parents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_parent_students_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rfid_cards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_uid = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: true),
                    card_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Permanent"),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rfid_cards", x => x.id);
                    table.ForeignKey(
                        name: "FK_rfid_cards_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bus_assignments_bus_id",
                table: "bus_assignments",
                column: "bus_id");

            migrationBuilder.CreateIndex(
                name: "IX_bus_assignments_driver_id",
                table: "bus_assignments",
                column: "driver_id");

            migrationBuilder.CreateIndex(
                name: "ix_buses_school_registration_number",
                table: "buses",
                columns: new[] { "school_id", "registration_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_classes_school_grade_division_year",
                table: "classes",
                columns: new[] { "school_id", "grade", "division", "academic_year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_drivers_school_license_number",
                table: "drivers",
                columns: new[] { "school_id", "license_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_drivers_school_user_id",
                table: "drivers",
                columns: new[] { "school_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_drivers_user_id",
                table: "drivers",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gps_devices_bus_id",
                table: "gps_devices",
                column: "bus_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gps_devices_school_device_code",
                table: "gps_devices",
                columns: new[] { "school_id", "device_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_parent_students_parent_student",
                table: "parent_students",
                columns: new[] { "parent_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_parent_students_student_id",
                table: "parent_students",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_parents_school_user_id",
                table: "parents",
                columns: new[] { "school_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_parents_user_id",
                table: "parents",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rfid_cards_school_card_uid",
                table: "rfid_cards",
                columns: new[] { "school_id", "card_uid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rfid_cards_student_id",
                table: "rfid_cards",
                column: "student_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rfid_devices_bus_id",
                table: "rfid_devices",
                column: "bus_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rfid_devices_school_device_code",
                table: "rfid_devices",
                columns: new[] { "school_id", "device_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_students_class_id",
                table: "students",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "ix_students_school_admission_number",
                table: "students",
                columns: new[] { "school_id", "admission_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bus_assignments");

            migrationBuilder.DropTable(
                name: "gps_devices");

            migrationBuilder.DropTable(
                name: "parent_students");

            migrationBuilder.DropTable(
                name: "rfid_cards");

            migrationBuilder.DropTable(
                name: "rfid_devices");

            migrationBuilder.DropTable(
                name: "drivers");

            migrationBuilder.DropTable(
                name: "parents");

            migrationBuilder.DropTable(
                name: "students");

            migrationBuilder.DropTable(
                name: "buses");

            migrationBuilder.DropTable(
                name: "classes");
        }
    }
}
