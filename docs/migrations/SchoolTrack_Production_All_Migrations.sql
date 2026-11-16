CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261112124830_InitialCreate') THEN
    CREATE TABLE schools (
        id uuid NOT NULL,
        name character varying(200) NOT NULL,
        code character varying(50) NOT NULL,
        time_zone_id character varying(100) NOT NULL DEFAULT 'UTC',
        contact_email character varying(256) NOT NULL,
        contact_phone character varying(50) NOT NULL,
        address character varying(500),
        is_active boolean NOT NULL DEFAULT TRUE,
        created_at timestamptz NOT NULL,
        updated_at timestamptz,
        CONSTRAINT "PK_schools" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261112124830_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_schools_code ON schools (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261112124830_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261112124830_InitialCreate', '10.0.9');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113054531_AddIdentityAndMultiTenancy') THEN
    CREATE TABLE audit_logs (
        id uuid NOT NULL,
        school_id uuid,
        user_id uuid,
        action character varying(100) NOT NULL,
        entity_type character varying(100) NOT NULL,
        entity_id character varying(100),
        details_json text,
        ip_address character varying(50),
        occurred_at timestamptz NOT NULL,
        created_at timestamptz NOT NULL,
        updated_at timestamptz,
        CONSTRAINT "PK_audit_logs" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113054531_AddIdentityAndMultiTenancy') THEN
    CREATE TABLE school_settings (
        id uuid NOT NULL,
        school_id uuid NOT NULL,
        duplicate_scan_window_seconds integer NOT NULL DEFAULT 60,
        device_offline_threshold_seconds integer NOT NULL DEFAULT 180,
        gps_retention_days integer NOT NULL DEFAULT 90,
        enable_sms_fallback boolean NOT NULL DEFAULT FALSE,
        created_at timestamptz NOT NULL,
        updated_at timestamptz,
        CONSTRAINT "PK_school_settings" PRIMARY KEY (id),
        CONSTRAINT "FK_school_settings_schools_school_id" FOREIGN KEY (school_id) REFERENCES schools (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113054531_AddIdentityAndMultiTenancy') THEN
    CREATE TABLE users (
        id uuid NOT NULL,
        school_id uuid,
        email character varying(256) NOT NULL,
        phone_number character varying(50),
        full_name character varying(150) NOT NULL,
        password_hash character varying(500) NOT NULL,
        role character varying(50) NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        last_login_at timestamptz,
        created_at timestamptz NOT NULL,
        updated_at timestamptz,
        CONSTRAINT "PK_users" PRIMARY KEY (id),
        CONSTRAINT "FK_users_schools_school_id" FOREIGN KEY (school_id) REFERENCES schools (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113054531_AddIdentityAndMultiTenancy') THEN
    CREATE TABLE refresh_tokens (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        token_hash character varying(128) NOT NULL,
        expires_at timestamptz NOT NULL,
        revoked_at timestamptz,
        replaced_by_token_hash character varying(128),
        created_by_ip character varying(50),
        created_at timestamptz NOT NULL,
        updated_at timestamptz,
        CONSTRAINT "PK_refresh_tokens" PRIMARY KEY (id),
        CONSTRAINT "FK_refresh_tokens_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113054531_AddIdentityAndMultiTenancy') THEN
    CREATE INDEX ix_audit_logs_school_id_occurred_at ON audit_logs (school_id, occurred_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113054531_AddIdentityAndMultiTenancy') THEN
    CREATE UNIQUE INDEX ix_refresh_tokens_token_hash ON refresh_tokens (token_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113054531_AddIdentityAndMultiTenancy') THEN
    CREATE INDEX "IX_refresh_tokens_user_id" ON refresh_tokens (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113054531_AddIdentityAndMultiTenancy') THEN
    CREATE UNIQUE INDEX ix_school_settings_school_id ON school_settings (school_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113054531_AddIdentityAndMultiTenancy') THEN
    CREATE UNIQUE INDEX ix_users_email ON users (email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113054531_AddIdentityAndMultiTenancy') THEN
    CREATE INDEX ix_users_school_id_role ON users (school_id, role);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113054531_AddIdentityAndMultiTenancy') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261113054531_AddIdentityAndMultiTenancy', '10.0.9');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE TABLE buses (
        id uuid NOT NULL,
        bus_number character varying(50) NOT NULL,
        registration_number character varying(50) NOT NULL,
        model character varying(100),
        capacity integer NOT NULL DEFAULT 30,
        maintenance_status character varying(50) NOT NULL DEFAULT 'Active',
        insurance_expiry timestamptz,
        fitness_expiry timestamptz,
        is_active boolean NOT NULL DEFAULT TRUE,
        created_at timestamptz NOT NULL,
        updated_at timestamptz,
        school_id uuid NOT NULL,
        CONSTRAINT "PK_buses" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE TABLE classes (
        id uuid NOT NULL,
        grade character varying(50) NOT NULL,
        division character varying(50) NOT NULL,
        academic_year character varying(20) NOT NULL,
        created_at timestamptz NOT NULL,
        updated_at timestamptz,
        school_id uuid NOT NULL,
        CONSTRAINT "PK_classes" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE TABLE drivers (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        license_number character varying(50) NOT NULL,
        license_expiry timestamptz,
        phone character varying(50) NOT NULL,
        availability_status character varying(50) NOT NULL DEFAULT 'Available',
        years_experience integer NOT NULL DEFAULT 0,
        rating double precision NOT NULL DEFAULT 5.0,
        created_at timestamptz NOT NULL,
        updated_at timestamptz,
        school_id uuid NOT NULL,
        CONSTRAINT "PK_drivers" PRIMARY KEY (id),
        CONSTRAINT "FK_drivers_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE TABLE parents (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        primary_phone character varying(50) NOT NULL,
        alternate_phone character varying(50),
        emergency_contact_name character varying(150),
        emergency_contact_phone character varying(50),
        address character varying(500),
        created_at timestamptz NOT NULL,
        updated_at timestamptz,
        school_id uuid NOT NULL,
        CONSTRAINT "PK_parents" PRIMARY KEY (id),
        CONSTRAINT "FK_parents_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE TABLE gps_devices (
        id uuid NOT NULL,
        device_code character varying(100) NOT NULL,
        bus_id uuid,
        imei character varying(50),
        model character varying(100),
        sim_card_number character varying(50),
        protocol_type character varying(50) NOT NULL DEFAULT 'Simulator',
        status character varying(50) NOT NULL DEFAULT 'Offline',
        last_seen_at timestamptz,
        last_location_at timestamptz,
        created_at timestamptz NOT NULL,
        updated_at timestamptz,
        school_id uuid NOT NULL,
        CONSTRAINT "PK_gps_devices" PRIMARY KEY (id),
        CONSTRAINT "FK_gps_devices_buses_bus_id" FOREIGN KEY (bus_id) REFERENCES buses (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE TABLE rfid_devices (
        id uuid NOT NULL,
        device_code character varying(100) NOT NULL,
        bus_id uuid,
        gate_location character varying(150),
        model character varying(100),
        ip_address character varying(50),
        port integer,
        protocol_type character varying(50) NOT NULL DEFAULT 'Simulator',
        status character varying(50) NOT NULL DEFAULT 'Offline',
        last_seen_at timestamptz,
        last_scan_at timestamptz,
        created_at timestamptz NOT NULL,
        updated_at timestamptz,
        school_id uuid NOT NULL,
        CONSTRAINT "PK_rfid_devices" PRIMARY KEY (id),
        CONSTRAINT "FK_rfid_devices_buses_bus_id" FOREIGN KEY (bus_id) REFERENCES buses (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE TABLE students (
        id uuid NOT NULL,
        class_id uuid NOT NULL,
        admission_number character varying(50) NOT NULL,
        first_name character varying(100) NOT NULL,
        last_name character varying(100) NOT NULL,
        photo_url character varying(500),
        gender character varying(20),
        date_of_birth timestamptz,
        emergency_contact_phone character varying(50),
        seat_number character varying(20),
        assigned_route_id uuid,
        assigned_stop_id uuid,
        is_active boolean NOT NULL DEFAULT TRUE,
        created_at timestamptz NOT NULL,
        updated_at timestamptz,
        school_id uuid NOT NULL,
        CONSTRAINT "PK_students" PRIMARY KEY (id),
        CONSTRAINT "FK_students_classes_class_id" FOREIGN KEY (class_id) REFERENCES classes (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE TABLE bus_assignments (
        id uuid NOT NULL,
        bus_id uuid NOT NULL,
        driver_id uuid NOT NULL,
        assigned_from timestamptz NOT NULL,
        assigned_to timestamptz,
        is_current boolean NOT NULL DEFAULT TRUE,
        created_at timestamptz NOT NULL,
        updated_at timestamptz,
        school_id uuid NOT NULL,
        CONSTRAINT "PK_bus_assignments" PRIMARY KEY (id),
        CONSTRAINT "FK_bus_assignments_buses_bus_id" FOREIGN KEY (bus_id) REFERENCES buses (id) ON DELETE CASCADE,
        CONSTRAINT "FK_bus_assignments_drivers_driver_id" FOREIGN KEY (driver_id) REFERENCES drivers (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE TABLE parent_students (
        id uuid NOT NULL,
        parent_id uuid NOT NULL,
        student_id uuid NOT NULL,
        relationship character varying(50) NOT NULL,
        is_primary_guardian boolean NOT NULL DEFAULT TRUE,
        can_authorize_pickup boolean NOT NULL DEFAULT TRUE,
        created_at timestamptz NOT NULL,
        updated_at timestamptz,
        school_id uuid NOT NULL,
        CONSTRAINT "PK_parent_students" PRIMARY KEY (id),
        CONSTRAINT "FK_parent_students_parents_parent_id" FOREIGN KEY (parent_id) REFERENCES parents (id) ON DELETE CASCADE,
        CONSTRAINT "FK_parent_students_students_student_id" FOREIGN KEY (student_id) REFERENCES students (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE TABLE rfid_cards (
        id uuid NOT NULL,
        card_uid character varying(50) NOT NULL,
        student_id uuid,
        card_type character varying(50) NOT NULL DEFAULT 'Permanent',
        status character varying(50) NOT NULL DEFAULT 'Active',
        issued_at timestamptz NOT NULL,
        expires_at timestamptz,
        created_at timestamptz NOT NULL,
        updated_at timestamptz,
        school_id uuid NOT NULL,
        CONSTRAINT "PK_rfid_cards" PRIMARY KEY (id),
        CONSTRAINT "FK_rfid_cards_students_student_id" FOREIGN KEY (student_id) REFERENCES students (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE INDEX "IX_bus_assignments_bus_id" ON bus_assignments (bus_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE INDEX "IX_bus_assignments_driver_id" ON bus_assignments (driver_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE UNIQUE INDEX ix_buses_school_registration_number ON buses (school_id, registration_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE UNIQUE INDEX ix_classes_school_grade_division_year ON classes (school_id, grade, division, academic_year);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE UNIQUE INDEX ix_drivers_school_license_number ON drivers (school_id, license_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE UNIQUE INDEX ix_drivers_school_user_id ON drivers (school_id, user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE UNIQUE INDEX "IX_drivers_user_id" ON drivers (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE UNIQUE INDEX "IX_gps_devices_bus_id" ON gps_devices (bus_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE UNIQUE INDEX ix_gps_devices_school_device_code ON gps_devices (school_id, device_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE UNIQUE INDEX ix_parent_students_parent_student ON parent_students (parent_id, student_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE INDEX "IX_parent_students_student_id" ON parent_students (student_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE UNIQUE INDEX ix_parents_school_user_id ON parents (school_id, user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE UNIQUE INDEX "IX_parents_user_id" ON parents (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE UNIQUE INDEX ix_rfid_cards_school_card_uid ON rfid_cards (school_id, card_uid);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE UNIQUE INDEX "IX_rfid_cards_student_id" ON rfid_cards (student_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE UNIQUE INDEX "IX_rfid_devices_bus_id" ON rfid_devices (bus_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE UNIQUE INDEX ix_rfid_devices_school_device_code ON rfid_devices (school_id, device_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE INDEX "IX_students_class_id" ON students (class_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    CREATE UNIQUE INDEX ix_students_school_admission_number ON students (school_id, admission_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113060444_AddCoreSchoolManagement') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261113060444_AddCoreSchoolManagement', '10.0.9');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113062421_AddRoutesAndTripEngine') THEN
    CREATE TABLE routes (
        id uuid NOT NULL,
        name character varying(100) NOT NULL,
        code character varying(50) NOT NULL,
        direction_type character varying(50) NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        created_at timestamptz NOT NULL,
        updated_at timestamptz,
        school_id uuid NOT NULL,
        CONSTRAINT "PK_routes" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113062421_AddRoutesAndTripEngine') THEN
    CREATE TABLE route_assignments (
        id uuid NOT NULL,
        route_id uuid NOT NULL,
        bus_id uuid NOT NULL,
        driver_id uuid NOT NULL,
        shift character varying(50) NOT NULL,
        effective_from timestamptz NOT NULL,
        effective_to timestamptz,
        is_active boolean NOT NULL DEFAULT TRUE,
        created_at timestamptz NOT NULL,
        updated_at timestamptz,
        school_id uuid NOT NULL,
        CONSTRAINT "PK_route_assignments" PRIMARY KEY (id),
        CONSTRAINT "FK_route_assignments_buses_bus_id" FOREIGN KEY (bus_id) REFERENCES buses (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_route_assignments_drivers_driver_id" FOREIGN KEY (driver_id) REFERENCES drivers (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_route_assignments_routes_route_id" FOREIGN KEY (route_id) REFERENCES routes (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113062421_AddRoutesAndTripEngine') THEN
    CREATE TABLE route_stops (
        id uuid NOT NULL,
        route_id uuid NOT NULL,
        stop_name character varying(100) NOT NULL,
        sequence_order integer NOT NULL,
        latitude numeric(10,7) NOT NULL,
        longitude numeric(10,7) NOT NULL,
        scheduled_offset_minutes integer NOT NULL DEFAULT 0,
        geofence_radius_meters integer NOT NULL DEFAULT 50,
        created_at timestamptz NOT NULL,
        updated_at timestamptz,
        school_id uuid NOT NULL,
        CONSTRAINT "PK_route_stops" PRIMARY KEY (id),
        CONSTRAINT "FK_route_stops_routes_route_id" FOREIGN KEY (route_id) REFERENCES routes (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113062421_AddRoutesAndTripEngine') THEN
    CREATE TABLE trips (
        id uuid NOT NULL,
        route_id uuid NOT NULL,
        bus_id uuid NOT NULL,
        driver_id uuid NOT NULL,
        trip_date date NOT NULL,
        shift character varying(50) NOT NULL,
        status character varying(50) NOT NULL DEFAULT 'Scheduled',
        scheduled_start_at timestamptz,
        actual_start_at timestamptz,
        actual_end_at timestamptz,
        cancellation_reason character varying(500),
        created_at timestamptz NOT NULL,
        updated_at timestamptz,
        school_id uuid NOT NULL,
        CONSTRAINT "PK_trips" PRIMARY KEY (id),
        CONSTRAINT "FK_trips_buses_bus_id" FOREIGN KEY (bus_id) REFERENCES buses (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_trips_drivers_driver_id" FOREIGN KEY (driver_id) REFERENCES drivers (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_trips_routes_route_id" FOREIGN KEY (route_id) REFERENCES routes (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113062421_AddRoutesAndTripEngine') THEN
    CREATE TABLE trip_status_history (
        id uuid NOT NULL,
        trip_id uuid NOT NULL,
        from_status character varying(50) NOT NULL,
        to_status character varying(50) NOT NULL,
        changed_by_user_id uuid NOT NULL,
        notes character varying(500),
        changed_at timestamptz NOT NULL,
        created_at timestamptz NOT NULL,
        updated_at timestamptz,
        school_id uuid NOT NULL,
        CONSTRAINT "PK_trip_status_history" PRIMARY KEY (id),
        CONSTRAINT "FK_trip_status_history_trips_trip_id" FOREIGN KEY (trip_id) REFERENCES trips (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113062421_AddRoutesAndTripEngine') THEN
    CREATE TABLE trip_stops (
        id uuid NOT NULL,
        trip_id uuid NOT NULL,
        route_stop_id uuid NOT NULL,
        sequence_order integer NOT NULL,
        status character varying(50) NOT NULL DEFAULT 'Pending',
        estimated_arrival_at timestamptz,
        actual_arrival_at timestamptz,
        actual_departure_at timestamptz,
        created_at timestamptz NOT NULL,
        updated_at timestamptz,
        school_id uuid NOT NULL,
        CONSTRAINT "PK_trip_stops" PRIMARY KEY (id),
        CONSTRAINT "FK_trip_stops_route_stops_route_stop_id" FOREIGN KEY (route_stop_id) REFERENCES route_stops (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_trip_stops_trips_trip_id" FOREIGN KEY (trip_id) REFERENCES trips (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113062421_AddRoutesAndTripEngine') THEN
    CREATE INDEX "IX_route_assignments_bus_id" ON route_assignments (bus_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113062421_AddRoutesAndTripEngine') THEN
    CREATE INDEX "IX_route_assignments_driver_id" ON route_assignments (driver_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113062421_AddRoutesAndTripEngine') THEN
    CREATE INDEX "IX_route_assignments_route_id" ON route_assignments (route_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113062421_AddRoutesAndTripEngine') THEN
    CREATE UNIQUE INDEX ix_route_stops_route_sequence ON route_stops (route_id, sequence_order);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113062421_AddRoutesAndTripEngine') THEN
    CREATE UNIQUE INDEX ix_routes_school_code ON routes (school_id, code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113062421_AddRoutesAndTripEngine') THEN
    CREATE INDEX ix_trip_status_history_trip_changed ON trip_status_history (trip_id, changed_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113062421_AddRoutesAndTripEngine') THEN
    CREATE INDEX "IX_trip_stops_route_stop_id" ON trip_stops (route_stop_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113062421_AddRoutesAndTripEngine') THEN
    CREATE UNIQUE INDEX ix_trip_stops_trip_sequence ON trip_stops (trip_id, sequence_order);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113062421_AddRoutesAndTripEngine') THEN
    CREATE INDEX ix_trips_bus_date_shift ON trips (bus_id, trip_date, shift);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113062421_AddRoutesAndTripEngine') THEN
    CREATE INDEX ix_trips_driver_date_shift ON trips (driver_id, trip_date, shift);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113062421_AddRoutesAndTripEngine') THEN
    CREATE INDEX "IX_trips_route_id" ON trips (route_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113062421_AddRoutesAndTripEngine') THEN
    CREATE INDEX ix_trips_school_date_shift ON trips (school_id, trip_date, shift);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113062421_AddRoutesAndTripEngine') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261113062421_AddRoutesAndTripEngine', '10.0.9');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113064547_AddGpsGatewayAndTelemetry') THEN
    ALTER TABLE gps_devices ADD api_key character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113064547_AddGpsGatewayAndTelemetry') THEN
    CREATE TABLE latest_bus_locations (
        id uuid NOT NULL,
        bus_id uuid NOT NULL,
        trip_id uuid,
        latitude numeric(10,7) NOT NULL,
        longitude numeric(10,7) NOT NULL,
        speed_kmh numeric(5,2) NOT NULL,
        heading numeric(5,2) NOT NULL,
        altitude numeric(7,2),
        accuracy_meters numeric(5,2),
        ignition_on boolean NOT NULL DEFAULT TRUE,
        is_online boolean NOT NULL DEFAULT TRUE,
        recorded_at timestamptz NOT NULL,
        created_at timestamptz NOT NULL,
        updated_at timestamptz,
        school_id uuid NOT NULL,
        CONSTRAINT "PK_latest_bus_locations" PRIMARY KEY (id),
        CONSTRAINT "FK_latest_bus_locations_buses_bus_id" FOREIGN KEY (bus_id) REFERENCES buses (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_latest_bus_locations_trips_trip_id" FOREIGN KEY (trip_id) REFERENCES trips (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113064547_AddGpsGatewayAndTelemetry') THEN
    CREATE TABLE location_logs (
        id uuid NOT NULL,
        bus_id uuid NOT NULL,
        trip_id uuid,
        latitude numeric(10,7) NOT NULL,
        longitude numeric(10,7) NOT NULL,
        speed_kmh numeric(5,2) NOT NULL,
        heading numeric(5,2) NOT NULL,
        altitude numeric(7,2),
        accuracy_meters numeric(5,2),
        ignition_on boolean NOT NULL DEFAULT TRUE,
        recorded_at timestamptz NOT NULL,
        received_at timestamptz NOT NULL,
        raw_payload text,
        created_at timestamptz NOT NULL,
        updated_at timestamptz,
        school_id uuid NOT NULL,
        CONSTRAINT "PK_location_logs" PRIMARY KEY (id),
        CONSTRAINT "FK_location_logs_buses_bus_id" FOREIGN KEY (bus_id) REFERENCES buses (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_location_logs_trips_trip_id" FOREIGN KEY (trip_id) REFERENCES trips (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113064547_AddGpsGatewayAndTelemetry') THEN
    CREATE UNIQUE INDEX "IX_latest_bus_locations_bus_id" ON latest_bus_locations (bus_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113064547_AddGpsGatewayAndTelemetry') THEN
    CREATE INDEX "IX_latest_bus_locations_school_id_is_online" ON latest_bus_locations (school_id, is_online);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113064547_AddGpsGatewayAndTelemetry') THEN
    CREATE INDEX "IX_latest_bus_locations_trip_id" ON latest_bus_locations (trip_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113064547_AddGpsGatewayAndTelemetry') THEN
    CREATE INDEX "IX_location_logs_bus_id_recorded_at" ON location_logs (bus_id, recorded_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113064547_AddGpsGatewayAndTelemetry') THEN
    CREATE INDEX "IX_location_logs_school_id_recorded_at" ON location_logs (school_id, recorded_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113064547_AddGpsGatewayAndTelemetry') THEN
    CREATE INDEX "IX_location_logs_trip_id_recorded_at" ON location_logs (trip_id, recorded_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113064547_AddGpsGatewayAndTelemetry') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261113064547_AddGpsGatewayAndTelemetry', '10.0.9');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113070657_AddRfidGatewayAndBoarding') THEN
    ALTER TABLE rfid_devices ADD api_key character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113070657_AddRfidGatewayAndBoarding') THEN
    ALTER TABLE rfid_devices ADD mac_address character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113070657_AddRfidGatewayAndBoarding') THEN
    CREATE TABLE boarding_events (
        id uuid NOT NULL,
        trip_id uuid NOT NULL,
        student_id uuid NOT NULL,
        rfid_card_id uuid NOT NULL,
        route_stop_id uuid,
        bus_id uuid NOT NULL,
        rfid_device_id uuid NOT NULL,
        scan_type character varying(50) NOT NULL,
        latitude numeric(10,7),
        longitude numeric(10,7),
        scanned_at timestamptz NOT NULL,
        recorded_at timestamptz NOT NULL,
        is_manual_override boolean NOT NULL DEFAULT FALSE,
        notes character varying(500),
        created_at timestamptz NOT NULL,
        updated_at timestamptz,
        school_id uuid NOT NULL,
        CONSTRAINT "PK_boarding_events" PRIMARY KEY (id),
        CONSTRAINT "FK_boarding_events_buses_bus_id" FOREIGN KEY (bus_id) REFERENCES buses (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_boarding_events_rfid_cards_rfid_card_id" FOREIGN KEY (rfid_card_id) REFERENCES rfid_cards (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_boarding_events_rfid_devices_rfid_device_id" FOREIGN KEY (rfid_device_id) REFERENCES rfid_devices (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_boarding_events_route_stops_route_stop_id" FOREIGN KEY (route_stop_id) REFERENCES route_stops (id) ON DELETE SET NULL,
        CONSTRAINT "FK_boarding_events_students_student_id" FOREIGN KEY (student_id) REFERENCES students (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_boarding_events_trips_trip_id" FOREIGN KEY (trip_id) REFERENCES trips (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113070657_AddRfidGatewayAndBoarding') THEN
    CREATE TABLE unknown_card_taps (
        id uuid NOT NULL,
        bus_id uuid,
        trip_id uuid,
        rfid_device_id uuid,
        device_code character varying(100) NOT NULL,
        card_uid character varying(100) NOT NULL,
        reason character varying(100) NOT NULL,
        latitude numeric(10,7),
        longitude numeric(10,7),
        scanned_at timestamptz NOT NULL,
        created_at timestamptz NOT NULL,
        updated_at timestamptz,
        school_id uuid NOT NULL,
        CONSTRAINT "PK_unknown_card_taps" PRIMARY KEY (id),
        CONSTRAINT "FK_unknown_card_taps_buses_bus_id" FOREIGN KEY (bus_id) REFERENCES buses (id) ON DELETE SET NULL,
        CONSTRAINT "FK_unknown_card_taps_rfid_devices_rfid_device_id" FOREIGN KEY (rfid_device_id) REFERENCES rfid_devices (id) ON DELETE SET NULL,
        CONSTRAINT "FK_unknown_card_taps_trips_trip_id" FOREIGN KEY (trip_id) REFERENCES trips (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113070657_AddRfidGatewayAndBoarding') THEN
    CREATE INDEX "IX_boarding_events_bus_id" ON boarding_events (bus_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113070657_AddRfidGatewayAndBoarding') THEN
    CREATE INDEX "IX_boarding_events_rfid_card_id" ON boarding_events (rfid_card_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113070657_AddRfidGatewayAndBoarding') THEN
    CREATE INDEX "IX_boarding_events_rfid_device_id" ON boarding_events (rfid_device_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113070657_AddRfidGatewayAndBoarding') THEN
    CREATE INDEX "IX_boarding_events_route_stop_id" ON boarding_events (route_stop_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113070657_AddRfidGatewayAndBoarding') THEN
    CREATE INDEX "IX_boarding_events_school_id_scanned_at" ON boarding_events (school_id, scanned_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113070657_AddRfidGatewayAndBoarding') THEN
    CREATE INDEX "IX_boarding_events_student_id_scanned_at" ON boarding_events (student_id, scanned_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113070657_AddRfidGatewayAndBoarding') THEN
    CREATE INDEX "IX_boarding_events_trip_id_student_id_scanned_at" ON boarding_events (trip_id, student_id, scanned_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113070657_AddRfidGatewayAndBoarding') THEN
    CREATE INDEX "IX_unknown_card_taps_bus_id" ON unknown_card_taps (bus_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113070657_AddRfidGatewayAndBoarding') THEN
    CREATE INDEX "IX_unknown_card_taps_card_uid" ON unknown_card_taps (card_uid);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113070657_AddRfidGatewayAndBoarding') THEN
    CREATE INDEX "IX_unknown_card_taps_rfid_device_id" ON unknown_card_taps (rfid_device_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113070657_AddRfidGatewayAndBoarding') THEN
    CREATE INDEX "IX_unknown_card_taps_school_id_scanned_at" ON unknown_card_taps (school_id, scanned_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113070657_AddRfidGatewayAndBoarding') THEN
    CREATE INDEX "IX_unknown_card_taps_trip_id" ON unknown_card_taps (trip_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261113070657_AddRfidGatewayAndBoarding') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261113070657_AddRfidGatewayAndBoarding', '10.0.9');
    END IF;
END $EF$;
COMMIT;

