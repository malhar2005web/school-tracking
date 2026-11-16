# Reusable Components & Architectural Decisions

This document defines exactly what concepts from `LOCATION-TRACKING` and `truck-fleet-managment` are **reused/adapted**, **rewritten**, or **discarded** in SchoolTrack, along with file references and technical rationale.

---

## 1. Concepts to Reuse & Adapt

| Concept | Reference File(s) | How SchoolTrack Adapts It | Rationale |
|---|---|---|---|
| **GPS Telemetry Fields & Coordinate Precision** | `LOCATION-TRACKING/backend/models/schema.sql` (`locations` table) | Store `Latitude`, `Longitude`, `SpeedKmh`, `Heading` (bearing), `AccuracyMeters`, `RecordedAt` (UTC), `ReceivedAt` (UTC) in `LocationLogs`. | Captures all kinematic fields needed for live map rendering, ETA calculation, and anomaly filtering. |
| **Composite Time-Series Indexing** | `LOCATION-TRACKING/backend/models/schema.sql` (`idx_locations_client_timestamp`) | Add composite indexes `(BusId, RecordedAt DESC)` and `(SchoolId, RecordedAt DESC)` on `LocationLogs`. | Supports fast latest-location queries and trip route playback as telemetry grows. |
| **Transactional Batch GPS Ingestion** | `LOCATION-TRACKING/backend/routes/locationRoutes.js` (`/batch`) | Implement `GpsIngestionService.IngestBatchAsync` using EF Core explicit database transactions (`BeginTransactionAsync`) with per-packet validation and idempotency keys. | Hardware trackers or cellular gateways buffer packets during dead zones and flush in batches upon reconnection. |
| **Device Health & Sync Metadata** | `LOCATION-TRACKING/backend/models/schema.sql` (`last_seen`, `pending_sync_count`, `last_synced_at`) & `truck-fleet-managment/backu/models/Driver.model.js` | Model dedicated `DeviceHeartbeats` and device `LastSeenAt` / `Status` fields on `GpsDevices` and `RfidDevices`. | Enables the Admin Dashboard ("Gateways: X/Y Online") and offline alert rules without coupling health to a user account. |
| **Latest Vehicle Position Projection** | `LOCATION-TRACKING/backend/routes/adminRoutes.js` (`LEFT JOIN LATERAL`) | Maintain a normalized `LatestBusLocations` table (1 row per `BusId`) updated atomically when a newer valid GPS event arrives. | Avoids scanning millions of `LocationLogs` rows during SignalR broadcasts and live fleet radar refreshes. |
| **Vehicle-to-Device Mapping** | `truck-fleet-managment/backu/models/DeviceTruckMapping.model.js` | Separate `GpsDevices` and `RfidDevices` linked to `Buses` via `SchoolId`-scoped assignments and backed by `DeviceCredentials`. | Prevents devices from self-declaring `BusId` or `SchoolId`; server resolves bus and school strictly from authenticated `DeviceId`. |
| **Driver & Vehicle Operational Entities** | `truck-fleet-managment/backu/models/Driver.model.js`, `Truck.model.js` | Rebuild as `Drivers` (linked to `ApplicationUser`) and `Buses` (registration number, capacity, maintenance/document status) scoped by `SchoolId`. | Cleanly separates driver identity from bus hardware and supports shift reassignments. |
| **Trip Assignment & Completion Lifecycle** | `truck-fleet-managment/backu/controller/admin.controller.js`, `driver.controller.js` | Rebuild as a formal Trip Engine with `Routes`, `RouteStops`, `RouteAssignments`, AM/PM `Trips`, `TripStops`, and `TripStatusHistory`. | School transit follows recurring stop-ordered routes with morning and afternoon runs rather than ad-hoc freight loads. |
| **MAUI Local Preferences / Offline Cache Pattern** | `LOCATION-TRACKING/maui-app/Services/Storage/SqliteStorageService.cs` | Use MAUI `SecureStorage` for JWT/Refresh tokens and SQLite/Preferences for non-sensitive cached trip/notification state. | Keeps mobile UX responsive on flaky school-zone networks while keeping tokens encrypted in OS keystore. |

---

## 2. Features to Rewrite Completely in C# / ASP.NET Core

| Feature | Reference Issue | SchoolTrack Rewrite |
|---|---|---|
| **Authentication & Token Lifecycle** | Cookie-only 30-day JWT (`generate.Token.js`), hardcoded secret fallbacks, open admin/driver signup routes. | ASP.NET Core Identity + short-lived JWT access tokens (e.g. 15 min) + hashed rotating refresh tokens (`RefreshTokens` table) + Role/Policy authorization + `SchoolId` tenant claims. |
| **Device Authentication & Provisioning** | Unauthenticated `POST /location/update` and unauthenticated `POST /truck/register-device`. | Admin-only device registration (`POST /api/v1/devices`) issuing rotatable device secrets/API keys (`DeviceCredentials`) or mTLS/HMAC signatures verified by `GpsDeviceAuthenticationService` and `RfidDeviceAuthenticationService`. |
| **Real-Time Telemetry & Alerts** | Unauthenticated Socket.IO with global `io.emit("tripUpdate")` leaking every vehicle to every client. | Authenticated ASP.NET Core SignalR hubs (`TrackingHub`) with strict group authorization: `school:{schoolId}:admins`, `bus:{busId}:authorized`, `parent:{parentId}`, `driver:{driverId}`. |
| **Trip State Machine** | Arbitrary string statuses and stubbed `updateTripStatusIfNeeded` in `driver.controller.js`. | Explicit finite state machine (`Scheduled -> Started -> InTransit -> Arrived -> Completed`, plus `Cancelled`) with transition guards and `TripStatusHistory` audit entries. |
| **Database Schema & Evolution** | Inline startup `ALTER TABLE` (`LOCATION-TRACKING`) and `sequelize.sync({ alter: true })` (`truck-fleet-managment`). | EF Core fluent `IEntityTypeConfiguration<T>` classes and versioned **EF Core Migrations** applied deliberately. |
| **Hardware Protocol Handling** | Assumes HTTP JSON from mobile/Cordova apps. | Hardware-agnostic `SchoolTrack.DeviceGateway` with `IGpsProtocolAdapter` and `IRfidProtocolAdapter` interfaces, normalized DTOs, deduplication, out-of-order guards, and simulators. |

---

## 3. Patterns & Code Explicitly Discarded

| Pattern / Artifact | Source | Why Discarded |
|---|---|---|
| **Driver Phone as Primary GPS Source** | `truck-fleet-managment/backu/controller/driver.controller.js` (`PUT /location`), `LOCATION-TRACKING/maui-app/Services/Gps/GpsService.cs` | SchoolTrack tracks the **bus** via dedicated onboard GPS hardware, independent of driver phone battery, permissions, or presence. |
| **Client Auto-Registration & Silent Device Reassignment on Login** | `LOCATION-TRACKING/backend/routes/clientRoutes.js` (`/login`) | Severe multi-tenant and spoofing risk; devices and users must be explicitly provisioned by School Admins. |
| **Unauthenticated Location Ingestion** | `LOCATION-TRACKING/backend/routes/locationRoutes.js` | Allows forged bus coordinates; all device ingestion must pass device authentication. |
| **Field-Sales CRM Tables & Endpoints** | `LOCATION-TRACKING` (`reminders`, `leaves`, `start_end_days`, `dsr_updates`) | Unrelated business domain. |
| **Truck Freight Fields & Cordova/React UIs** | `truck-fleet-managment` (`load_type`, `load_weight`, `cordova-app/`, `admin-web/`) | Replaced by SchoolTrack domain entities, Blazor Admin Web, and .NET MAUI Blazor Hybrid apps matching the Stitch design system. |

