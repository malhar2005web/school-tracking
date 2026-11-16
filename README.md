# SchoolTrack — Enterprise Multi-School Transportation & Live Student Safety Platform

[![.NET 10 LTS](https://img.shields.io/badge/.NET-10.0%20LTS-purple.svg)](https://dotnet.microsoft.com/)
[![PostgreSQL 16](https://img.shields.io/badge/PostgreSQL-16-blue.svg)](https://www.postgresql.org/)
[![EF Core](https://img.shields.io/badge/EF%20Core-10.0-blue.svg)](https://learn.microsoft.com/ef/core/)
[![SignalR](https://img.shields.io/badge/SignalR-Real--Time-orange.svg)](https://dotnet.microsoft.com/apps/aspnet/signalr)
[![Blazor Server](https://img.shields.io/badge/Blazor-Claymorphic%20UI-512BD4.svg)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-Blazor%20Hybrid-purple.svg)](https://dotnet.microsoft.com/apps/maui)
[![Tests Passing](https://img.shields.io/badge/Tests-102%2F102%20Passing-brightgreen.svg)]()

> **SchoolTrack** is a production-grade, multi-school transportation logistics and real-time student safety tracking platform built strictly on **C# / .NET 10 LTS**, **PostgreSQL 16/17**, **SignalR**, **Blazor Server Admin Web**, and **.NET MAUI Blazor Hybrid**.

---

## 🌐 Live Deployment & Demonstration

| Component | Endpoint | Description |
|---|---|---|
| **Admin Web Portal (Port 5050)** | `http://173.249.59.181:5050/` | Interactive Blazor Fleet Management & Live Radar |
| **Admin Web Portal (Default 80)** | `http://173.249.59.181/` | Primary HTTP gateway for transport directors |
| **REST API Swagger Documentation** | `http://173.249.59.181:5050/swagger` | OpenAPI v3 interactive endpoint explorer |
| **System Health Telemetry** | `http://173.249.59.181:5050/health` | Live PostgreSQL, EF Core & Kestrel probes |

### Demo Credentials
* **Role: School Admin (Springfield Public School)**
  * **Email:** `admin@springfield.edu`
  * **Password:** `Admin@SchoolTrack2026!`
* **Role: Platform Super Admin**
  * **Email:** `superadmin@schooltrack.com`
  * **Password:** `Admin@SchoolTrack2026!`

---

## 🎯 Key Architectural Principles

1. **100% C# / .NET Ecosystem**: Zero React, Angular, Vue, or Node.js. Shared domain contracts and models span seamlessly from the database engine to native mobile tablets.
2. **Dedicated Hardware Ingestion (Zero Driver Dependency for Safety)**: Physical bus location tracking and student RFID tap detection are performed strictly by dedicated, vehicle-mounted hardware (dedicated cellular GPS trackers and dedicated bus door Ethernet/PoE RFID readers). Telemetry streams directly to `SchoolTrack.DeviceGateway`. If a driver forgets their tablet or loses cellular data, student tracking remains 100% active.
3. **Strict Multi-Tenant Isolation**: Tenant boundaries (`SchoolId`) are enforced at the database persistence layer using EF Core Global Query Filters. School A users cannot query or mutate School B data under any circumstances.
4. **Claymorphic Stitch UI System**: Tactile, high-visibility user interfaces featuring rounded pill badges, layered clay drop shadows, safety mint `#0C6B55` primary actions, transit yellow `#FFD66B` alerts, and coral red `#BA1A1A` emergency signals.
5. **Resilient Hardware Protocol Handling**: Kinematic GPS speed profiles, dead-zone offline store-and-forward batch ingestion, 15-second rapid double-tap deduplication cooldown, and rogue RFID card security audit logging.

---

## 🏗️ System Architecture

```
                                  +---------------------------------------+
                                  |       DEDICATED BUS HARDWARE          |
                                  |  [GPS Tracker]     [Door RFID Reader] |
                                  +-------+-------------------+-----------+
                                          | Cellular APN      | PoE/Cellular
                                          v                   v
+-------------------------------+ +---------------------------------------+ +-------------------------------+
|    Blazor Admin Web App       | |       SchoolTrack.DeviceGateway       | |   Driver MAUI Mobile App      |
|    (Transport Directors)      | |   (Kinematics, Buffering, Gateway)    | | (In-Cab HUD, Turn-by-Turn)    |
|   • Fleet Management          | +-------------------+-------------------+ |   • CDL Pre-Trip Inspection     |
|   • Live Fleet Radar          |                     |                     |   • Student Boarding Roster     |
|   • Route & Stop Planning     |                     |                     |   • Delay Broadcast & SOS       |
+---------------+---------------+                     |                     +---------------+---------------+
                |                                     |                                     |
                +---------------------+               |               +---------------------+
                                      v               v               v
                          +-------------------------------------------------------+
                          |              ASP.NET Core Web API (NET 10)            |
                          |  • Modular Monolith with Clean Architecture           |
                          |  • JWT Authentication & Custom RBAC Policies          |
                          |  • Real-Time SignalR Tracking Hub (/hubs/tracking)    |
                          |  • Native Partition-Based Rate Limiting               |
                          |  • Security Headers & RFC 7807 Error Handling         |
                          +---------------------------+---------------------------+
                                                      |
                                    +-----------------+-----------------+
                                    |                                   |
                                    v                                   v
                         +--------------------+              +--------------------+
                         |   PostgreSQL 16    |              | Parent MAUI Mobile |
                         | (EF Core Multi-Ten |              | (Live Bus Radar,   |
                         |  Global Filters)   |              |  Dynamic ETAs,     |
                         +--------------------+              |  Boarding Alerts)  |
                                                             +--------------------+
```

---

## 📦 Solution Structure

The solution is organized in `SchoolTrack.slnx` across 11 projects:

| Project | Target | Description |
|---|---|---|
| `src/SchoolTrack.Shared` | `net10.0` | Cross-platform DTOs, API route constants, role claims, and result envelopes |
| `src/SchoolTrack.Domain` | `net10.0` | Enterprise entities (`School`, `User`, `Bus`, `Driver`, `Route`, `Student`, `RfidCard`, `GpsDevice`) |
| `src/SchoolTrack.Application` | `net10.0` | Clean Architecture use cases, handlers, validations, and query/command services |
| `src/SchoolTrack.Infrastructure` | `net10.0` | PostgreSQL persistence (`SchoolTrackDbContext`), EF Core migrations, JWT token service |
| `src/SchoolTrack.DeviceGateway` | `net10.0` | Dedicated GPS & RFID ingestion, deduplication cooldown, store-and-forward batch buffering |
| `src/SchoolTrack.Api` | `net10.0` | ASP.NET Core REST API, SignalR Tracking Hub, rate limiting, and security middleware |
| `src/SchoolTrack.Admin` | `net10.0` | Interactive Blazor Admin Web Portal featuring Claymorphic Stitch UI and Leaflet live radar |
| `src/SchoolTrack.ParentApp` | `net10.0-android` / MAUI | Parent mobile app: Live Bus Radar, dynamic ETA countdown, boarding alerts, RFID history |
| `src/SchoolTrack.DriverApp` | `net10.0-android` / MAUI | In-cab driver console: Navigation HUD, RFID door roster, delay broadcasting, 911 SOS |
| `tools/SchoolTrack.HardwareSimulator` | `net10.0` | Interactive CLI tool & automation harness for kinematic GPS and Ethernet RFID simulation |
| `tests/SchoolTrack.Api.Tests` | `net10.0` | 102 automated unit and integration tests (100% passing) |

---

## 👥 User Personas & Applications

### 1. School Administrators & Transport Directors (`SchoolTrack.Admin`)
- **Institution Onboarding**: Automated school provisioning with isolated settings and default administration credentials.
- **Fleet Management**: Register buses, monitor chassis/capacity specs, and manage active driver assignments with rotation history.
- **Route Planning**: Map routes, define geofenced waypoint stops (AM/PM shifts), and calculate transit times.
- **Student & Badge Enrollment**: Manage student records, link authorized guardians, and provision 13.56 MHz RFID card UIDs.
- **Live Fleet Radar**: Real-time Leaflet map displaying active buses, live speed/heading indicators, and door scan feeds.
- **Audit Logs & Security**: Review unknown/rogue card taps, driver safety checklists, and emergency SOS incident alerts.

### 2. School Bus Drivers (`SchoolTrack.DriverApp`)
- **In-Cab Verification**: Commercial Driver's License (CDL) login and terminal PIN verification.
- **Pre-Trip Safety Inspection**: Mandatory 6-point tactile vehicle safety checklist (brakes, flashers, tires, emergency exits, fire extinguisher, seat belts).
- **Turn-by-Turn In-Cab HUD**: Dynamic directional navigation with speed limit indicators and stop approach alerts.
- **Door RFID Reader Roster**: Live synchronization with the bus door reader; automatic boarding notifications with seat assignment alerts.
- **Delay Broadcast & SOS Trigger**: Instant notification to transport dispatch and waiting parents of traffic delays or road hazards.

### 3. Parents & Guardians (`SchoolTrack.ParentApp`)
- **Real-Time GPS Bus Radar**: Live location of the assigned bus along the morning and afternoon routes.
- **Dynamic ETA Countdown**: Sub-minute arrival countdowns calculated dynamically from speed, traffic, and remaining stops.
- **RFID Boarding & Deboarding Push Alerts**: Instant notification when a child taps their badge entering or exiting the bus.
- **Child Geofence & Safe-Zone Notifications**: Automated boundary arrival alerts for peace of mind.

---

## 🚀 Quickstart & Local Development

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL 16+](https://www.postgresql.org/download/)
- [Java 21 JDK](https://adoptium.net/) & [Android SDK](https://developer.android.com/studio) (for mobile APK builds)

### 1. Database Setup
```bash
# Clone the repository
git clone https://github.com/malhar2005web/school-tracking.git
cd school-tracking

# Apply the all-in-one idempotent database migration script
psql -U postgres -d schooltrack_db -f docs/migrations/SchoolTrack_Production_All_Migrations.sql
```

### 2. Run Locally via .NET CLI

#### Start the ASP.NET Core Web API:
```bash
cd src/SchoolTrack.Api
dotnet run
```
*API Swagger will be available at: `http://localhost:5000/swagger`*

#### Start the Blazor Admin Web Portal:
```bash
cd src/SchoolTrack.Admin
dotnet run
```
*Admin Dashboard will be available at: `http://localhost:5200/`*

#### Run the Standalone Hardware Simulator:
```bash
# Interactive mode with ANSI terminal UI
dotnet run --project tools/SchoolTrack.HardwareSimulator

# Automated Golden-Path Simulation
dotnet run --project tools/SchoolTrack.HardwareSimulator -- --mode golden --bus BUS-104
```

### 3. Execute the Automated Test Suite
```bash
dotnet test tests/SchoolTrack.Api.Tests/SchoolTrack.Api.Tests.csproj
```
**Test Results**: **102/102 Passing (100%)** across all unit, integration, multi-tenant isolation, real-time SignalR, and hardware simulation suites.

---

## 📱 Building Android APKs (.NET MAUI)

To generate standalone Android APK binaries:

```powershell
# 1. Set environment variables
$env:JAVA_HOME = "C:\Program Files\Android\Android Studio\jbr"
$env:ANDROID_HOME = "C:\Users\<user>\AppData\Local\Android\Sdk"

# 2. Build Parent App APK
dotnet publish src/SchoolTrack.ParentApp/SchoolTrack.ParentApp.csproj `
  -f net10.0-android `
  -c Release `
  -p:AndroidPackageFormat=apk `
  -o ./apk/parent

# 3. Build Driver Console APK
dotnet publish src/SchoolTrack.DriverApp/SchoolTrack.DriverApp.csproj `
  -f net10.0-android `
  -c Release `
  -p:AndroidPackageFormat=apk `
  -o ./apk/driver
```

---

## 🛡️ Production Deployment & Hardening

1. **Security Headers & Defense-in-Depth**:
   - `X-Content-Type-Options: nosniff`
   - `X-Frame-Options: SAMEORIGIN`
   - `X-XSS-Protection: 1; mode=block`
   - `Referrer-Policy: strict-origin-when-cross-origin`
   - `Strict-Transport-Security: max-age=31536000; includeSubDomains`
   - Server identification banners (`Server`, `X-Powered-By`) stripped.
2. **Partitioned Rate Limiting**:
   - `AuthPolicy`: 15 requests / min (brute-force defense)
   - `HardwarePolicy`: 150 requests / min (high-frequency GPS telemetry ingestion)
   - `GeneralPolicy`: 100 requests token-bucket with burst capacity
3. **Nginx Reverse Proxy & SignalR WebSockets**:
   Reverse proxies Port 80 and Port 5050 to internal Kestrel daemon services (`127.0.0.1:5200` Admin and `127.0.0.1:5000` API) with full HTTP/1.1 WebSocket upgrade preservation.

---

## 📚 Documentation Index

- [System User Manual & Operational Handbook](docs/SYSTEM_USER_MANUAL.md) — Comprehensive guide for School Admins, Drivers, Parents, and Hardware Engineers
- [Production Deployment Guide](docs/PRODUCTION_DEPLOYMENT_GUIDE.md) — Docker Compose, Nginx, SSL, rate limiting, and backup automation
- [Architecture & Design Decisions](docs/ARCHITECTURE.md) — Clean Architecture Modular Monolith and data flows
- [Database Schema & ERD](docs/DATABASE_DESIGN.md) — PostgreSQL schema, multi-tenant global filters, and relationships
- [Hardware Integration Guide](docs/HARDWARE_INTEGRATION.md) — Ingestion protocols for GPS trackers and Ethernet RFID readers
- [Stitch UI System Analysis](docs/STITCH_UI_ANALYSIS.md) — Claymorphic safety tokens, color palette, and styling specs
- [Reusable Component Index](docs/REUSABLE_COMPONENTS.md) — Cross-platform UI cards, badges, and controls
- [Phase Progress Log](docs/PHASE_PROGRESS.md) — 12-phase delivery log and automated test records

---

## 📄 License & Copyright
Developed for enterprise school transportation safety management. All rights reserved.
