# Repository Analysis

This document records the factual inspection of the two reference repositories prior to designing and implementing **SchoolTrack** in C#/.NET.

---

## 1. Inspected Repositories

| Alias | GitHub URL | Primary Stack | Primary Purpose |
|---|---|---|---|
| **Repository A (`LOCATION-TRACKING`)** | `https://github.com/malhar2005web/LOCATION-TRACKING` | Node.js (Express 4.21), `pg` (raw SQL), PostgreSQL, .NET MAUI Blazor Hybrid (`maui-app/`), Cordova (`cordova-app/`), Static HTML/CSS/JS (`admin-web-portal/`) | Field-staff GPS location tracking, offline batch sync, workday start/end, reminders, leaves, and Daily Sales Reports (DSR) |
| **Repository B (`truck-fleet-managment`)** | `https://github.com/malhar2005web/truck-fleet-managment` | Node.js (Express 4.18 ESM), Sequelize 6, PostgreSQL, Socket.IO 4.7, Twilio SDK, React/Vite (`admin-web/`), React Native/Expo (`mobile-app/`), Cordova (`cordova-app/`) | Concrete/truck fleet dispatch (`backu/`): driver & truck registry, device-to-truck mapping, trip assignment, driver GPS updates, real-time Socket.IO events |

---

## 2. Repository A (`LOCATION-TRACKING`) — Detailed Findings

### Inspected Files
- `backend/server.js`
- `backend/config/db.js`
- `backend/models/schema.sql`
- `backend/middleware/auth.js`
- `backend/utils/helpers.js`
- `backend/routes/locationRoutes.js`
- `backend/routes/clientRoutes.js`
- `backend/routes/adminRoutes.js`
- `maui-app/Services/Gps/GpsService.cs`
- `maui-app/Services/Storage/SqliteStorageService.cs`

### Observed Implementation & Gaps
1. **PostgreSQL Connection & Schema (`backend/config/db.js`, `backend/models/schema.sql`)**:
   - Uses `pg.Pool` and stores GPS history in `locations` (`BIGSERIAL`, `client_id`, `device_id`, `latitude DECIMAL(10,7)`, `longitude DECIMAL(10,7)`, `accuracy`, `speed`, `bearing`, `battery_level`, `timestamp`, `created_at`) with composite index `idx_locations_client_timestamp (client_id, timestamp DESC)`.
   - **Gap / Anti-Pattern**: `db.js` runs inline `ALTER TABLE` and `CREATE TABLE IF NOT EXISTS` statements automatically on server startup instead of versioned migrations.
   - **Gap**: `clients` mixes user identity and device binding (`client_id`, `device_id`, `last_seen`, `pending_sync_count`, `last_synced_at`) without multi-tenant (`SchoolId`) isolation.
2. **GPS Location Ingestion (`backend/routes/locationRoutes.js`)**:
   - `POST /api/location/update` validates presence of `clientId`, `deviceId`, `latitude`, `longitude` and checks ranges (`-90..90`, `-180..180`), inserts into `locations`, and updates `clients.last_seen`, `last_synced_at`, `pending_sync_count`.
   - `POST /api/location/batch` wraps batch inserts in a PostgreSQL transaction (`BEGIN` / `COMMIT` / `ROLLBACK`) for offline sync.
   - **Critical Security Gap**: `verifyClient` is imported on line 4 of `locationRoutes.js` **but never attached** to `/update` or `/batch`. Any anonymous caller can inject arbitrary GPS coordinates for any `clientId`.
   - **Validation Gap**: `/batch` skips coordinate range validation and timestamp sanity checks.
3. **Latest Location & History Queries (`backend/routes/adminRoutes.js`)**:
   - `GET /api/admin/clients` uses PostgreSQL `LEFT JOIN LATERAL (... ORDER BY timestamp DESC LIMIT 1)` to retrieve each client's latest location efficiently.
   - `GET /api/admin/client/:id` retrieves up to the latest 100 location records ordered by `timestamp DESC`.
4. **Authentication & Client Registration (`backend/routes/clientRoutes.js`, `backend/middleware/auth.js`, `backend/utils/helpers.js`)**:
   - Uses JWT (`24h` expiry) with bcrypt password hashing (`admins`) and role checks (`verifyClient`, `verifyAdmin`).
   - **Critical Security Gaps**:
     - `POST /api/client/login` **auto-creates** unknown `clientId` records (`INSERT INTO clients ...`) and **silently reassigns `device_id`** when `is_active` is false.
     - `helpers.js` contains a hardcoded JWT secret fallback (`'fallback-secret-key'`).
     - No refresh token rotation, no school/tenant scoping, no rate limiting.
5. **MAUI Reference (`maui-app/`)**:
   - `SqliteStorageService.cs` demonstrates SQLite local queue persistence (`IsSynced` flag) and `Preferences` storage.
   - `GpsService.cs` uses device phone GPS (`Geolocation.Default`) — **explicitly NOT usable as SchoolTrack's primary bus GPS source** (SchoolTrack uses dedicated bus GPS hardware).

---

## 3. Repository B (`truck-fleet-managment/backu`) — Detailed Findings

### Inspected Files
- `backu/server.js`
- `backu/config/db.js`
- `backu/config/envVars.js`
- `backu/models/index.js`
- `backu/models/AdminUser.model.js`
- `backu/models/Driver.model.js`
- `backu/models/Truck.model.js`
- `backu/models/Trip.model.js`
- `backu/models/TripLog.model.js`
- `backu/models/DeviceTruckMapping.model.js`
- `backu/models/Session.model.js`
- `backu/models/OTPSession.model.js`
- `backu/middleware/protectRoute.js`
- `backu/utils/generate.Token.js`
- `backu/utils/socket.js`
- `backu/controller/auth.controller.js`
- `backu/controller/admin.controller.js`
- `backu/controller/driver.controller.js`
- `backu/routes/auth.route.js`
- `backu/routes/admin.route.js`
- `backu/routes/driver.route.js`

### Observed Implementation & Gaps
1. **Schema & ORM (`backu/server.js`, `backu/models/*`)**:
   - Defines `AdminUser` (UUID PK), `Driver`, `Truck`, `Trip`, `TripLog`, `DeviceTruckMapping`, `Session`, and `OTPSession` using Sequelize.
   - **Critical Operational Gap**: `backu/server.js` executes `sequelize.sync({ alter: true })` at startup, which is unsafe for production databases.
   - **Model Gaps**:
     - `TripLog` has no timestamp column (`timestamps: false`, no `recorded_at`), making time-ordered route replay impossible.
     - `driver.controller.js` (`updateLocation`) sets `truck.lastLat` and `truck.lastLng`, which do not exist on `Truck.model.js`.
     - No multi-school/tenant (`SchoolId`) foreign keys anywhere.
2. **Authentication & Device Registration (`backu/controller/auth.controller.js`, `protectRoute.js`, `generate.Token.js`)**:
   - Issues 30-day JWTs persisted in `sessions` table and delivered via `truck-fleet-auth` HTTP-only cookie (`protectRoute.js` verifies both JWT signature and active `Session` row).
   - **Critical Security Gaps**:
     - `POST /api/v1/auth/admin/signup` and `POST /api/v1/auth/driver/register` are open unauthenticated endpoints.
     - `POST /api/v1/auth/truck/register-device` and `GET /api/v1/auth/truck/check-device` are **completely unauthenticated**, auto-create trucks on the fly, and overwrite existing `DeviceTruckMapping.truck_id` assignments.
     - `envVars.js` uses hardcoded JWT fallback `"replace_with_secret"`.
     - Cookies are incompatible with native mobile MAUI Bearer token + refresh token rotation flows.
     - `twilio` is imported in `auth.controller.js`, and `OTPSession` model exists, but no OTP routes are wired.
3. **Trip Lifecycle & Fleet Management (`admin.controller.js`, `driver.controller.js`)**:
   - Supports admin creating drivers, trucks, and trips (`assigned`), driver accepting (`accepted`) and completing (`completed`) trips, and admin updating trip status / viewing driver history and active trip.
   - **Gaps**:
     - No explicit state-transition validation (admin `updateTripStatus` accepts any arbitrary string; `updateTripStatusIfNeeded` in `driver.controller.js` is an empty stub).
     - No `Route`, `RouteStop`, morning/afternoon trip separation, or `TripStatusHistory` audit trail.
     - Location updates come from the driver's mobile app (`PUT /api/v1/driver/location`) rather than dedicated bus GPS hardware.
4. **Real-Time Communication (`backu/utils/socket.js`)**:
   - Uses Socket.IO with in-memory `driverSockets` Map and `adminSockets` Set.
   - **Critical Security Gaps**:
     - Socket connections are unauthenticated (`registerDriver` trusts any client-supplied `driverId`; `registerAdmin` allows anyone to join `adminSockets`).
     - `publishTripUpdate` calls `io.emit("tripUpdate", payload)`, broadcasting every vehicle's coordinates to **all** connected clients globally.

---

## 4. Feature-by-Feature Verification Matrix

| Source Repository | Feature | Existing Implementation | SchoolTrack Usage | Action |
|---|---|---|---|---|
| `LOCATION-TRACKING` | GPS ingestion | `POST /api/location/update` (unauthenticated `locationRoutes.js`) | `GpsIngestionService` behind `IGpsProtocolAdapter` with device auth | **Reimplement** |
| `LOCATION-TRACKING` | Location history | `locations` PostgreSQL table (`BIGSERIAL`, `(client_id, timestamp DESC)` index) | `LocationLogs` table (`SchoolId`, `BusId`, `GpsDeviceId`, `RecordedAt`) | **Adapt** |
| `LOCATION-TRACKING` | Latest location lookup | `LEFT JOIN LATERAL` query in `adminRoutes.js` | Dedicated `LatestBusLocations` table/projection updated transactionally | **Adapt** |
| `LOCATION-TRACKING` | Batch / offline sync | `POST /api/location/batch` with SQL transaction + `pending_sync_count` | Idempotent batch ingestion in `GpsLocationProcessor` with per-item validation | **Adapt** |
| `LOCATION-TRACKING` | GPS data validation | Lat `[-90,90]` / Lng `[-180,180]` check in `/update` only | Strict validation (coords, timestamp bounds, duplicate/out-of-order checks) | **Reimplement** |
| `LOCATION-TRACKING` | Client auto-reg & reassignment | `POST /api/client/login` auto-creates clients & swaps `device_id` | Explicitly prohibited; admin-only device provisioning | **Discard** |
| `LOCATION-TRACKING` | Startup self-migrations | Inline `ALTER TABLE` / `CREATE TABLE` in `db.js` | EF Core Migrations only | **Discard** |
| `LOCATION-TRACKING` | CRM / Field-sales modules | `reminders`, `leaves`, `start_end_days`, `dsr_updates` | Not part of SchoolTrack domain | **Discard** |
| `truck-fleet-managment` | Driver management | `Driver` model + `createDriver`/`listDrivers` | `Drivers` entity scoped by `SchoolId` with ASP.NET Identity user link | **Adapt** |
| `truck-fleet-managment` | Vehicle management | `Truck` model + `listTrucks` with device join | `Buses` entity (`RegistrationNumber`, `Capacity`, maintenance status, `SchoolId`) | **Adapt** |
| `truck-fleet-managment` | Device-to-vehicle mapping | `DeviceTruckMapping` (`device_id` <-> `truck_id`) | `GpsDevices`, `RfidDevices`, `DeviceCredentials`, `Bus` mapping | **Reimplement** |
| `truck-fleet-managment` | Trip creation & lifecycle | `Trip` model + `createTrip`, `acceptTrip`, `completeTrip` | Trip engine with `Routes`, `RouteStops`, AM/PM trips, validated state machine | **Reimplement** |
| `truck-fleet-managment` | Historical trip logs | `TripLog` model (lacks timestamp) | `TripStatusHistory`, `LocationLogs`, `BoardingEvents` with UTC timestamps | **Reimplement** |
| `truck-fleet-managment` | Real-time tracking | Unauthenticated Socket.IO global `io.emit` | Authenticated ASP.NET Core SignalR hubs with school/parent/driver group isolation | **Replace** |
| `truck-fleet-managment` | Session & auth | 30-day cookie JWT + `Session` table + open signup/device-reg | ASP.NET Core Identity + short-lived JWT + rotating Refresh Tokens + Device Keys | **Replace** |
| `truck-fleet-managment` | Driver phone GPS source | `PUT /api/v1/driver/location` updates truck/trip lat-lng | Replaced by dedicated bus GPS hardware gateway (phone GPS prohibited as primary) | **Discard** |
| `truck-fleet-managment` | Startup `sequelize.sync({ alter: true })` | Automatic schema alteration in `backu/server.js` | EF Core Migrations only | **Discard** |

