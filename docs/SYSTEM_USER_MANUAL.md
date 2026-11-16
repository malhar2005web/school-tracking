# SchoolTrack — Comprehensive System User Manual & Operational Handbook

Welcome to the **SchoolTrack Enterprise Transportation Management & Student Safety Platform** operational handbook. This manual provides detailed operational instructions, architectural guidelines, safety protocols, and configuration references for all user personas:
1. **School Administrators & Transport Directors** (Blazor Admin Web App)
2. **School Bus Drivers** (In-Cab .NET MAUI Blazor Hybrid Tablet/Mobile Console)
3. **Parents & Guardians** (Parent .NET MAUI Blazor Hybrid Mobile App)
4. **Hardware Engineers, Integrators & Transport Dispatchers** (Dedicated IoT Gateways & Simulators)

---

## Table of Contents
1. [Platform Overview & Design Architecture](#1-platform-overview--design-architecture)
2. [Role 1: School Administrators & Transport Directors](#2-role-1-school-administrators--transport-directors)
   - [2.1 Authentication & Multi-Tenant Setup](#21-authentication--multi-tenant-setup)
   - [2.2 Fleet Management (Buses & Driver Rotation)](#22-fleet-management-buses--driver-rotation)
   - [2.3 Route & Stop Configuration](#23-route--stop-configuration)
   - [2.4 Student & Guardian Enrollment](#24-student--guardian-enrollment)
   - [2.5 RFID Badge Provisioning & Hardware Device Registration](#25-rfid-badge-provisioning--hardware-device-registration)
   - [2.6 Real-Time Live Fleet Radar](#26-real-time-live-fleet-radar)
   - [2.7 Audit Logs, Safety Alerts & Compliance Reporting](#27-audit-logs-safety-alerts--compliance-reporting)
3. [Role 2: School Bus Drivers (In-Cab Console)](#3-role-2-school-bus-drivers-in-cab-console)
   - [3.1 In-Cab Driver Sign-In & Verification](#31-in-cab-driver-sign-in--verification)
   - [3.2 Pre-Trip CDL Safety Inspection Checklist](#32-pre-trip-cdl-safety-inspection-checklist)
   - [3.3 Live In-Cab Turn-by-Turn Navigation & HUD](#33-live-in-cab-turn-by-turn-navigation--hud)
   - [3.4 Student Boarding Roster & Door RFID Synchronization](#34-student-boarding-roster--door-rfid-synchronization)
   - [3.5 Manual Check-In Overrides & Absence Logging](#35-manual-check-in-overrides--absence-logging)
   - [3.6 In-Cab Delay Broadcast System](#36-in-cab-delay-broadcast-system)
   - [3.7 Emergency SOS Incident Protocol](#37-emergency-sos-incident-protocol)
   - [3.8 Shift Completion & Vehicle Sign-Off](#38-shift-completion--vehicle-sign-off)
4. [Role 3: Parents & Guardians (Parent App)](#4-role-3-parents--guardians-parent-app)
   - [4.1 Secure Mobile Authentication & Biometrics](#41-secure-mobile-authentication--biometrics)
   - [4.2 Multi-Child Profile Switcher](#42-multi-child-profile-switcher)
   - [4.3 Live Bus Radar & Predictive Dynamic ETA](#43-live-bus-radar--predictive-dynamic-eta)
   - [4.4 Interactive Route & Vehicle Map](#44-interactive-route--vehicle-map)
   - [4.5 Real-Time Boarding & Deboarding Push Timeline](#45-real-time-boarding--deboarding-push-timeline)
   - [4.6 Student RFID Scan History Verification](#46-student-rfid-scan-history-verification)
   - [4.7 Absence Submission & Driver Notice](#47-absence-submission--driver-notice)
   - [4.8 Direct Driver & School Dispatch Communication](#48-direct-driver--school-dispatch-communication)
5. [Role 4: Hardware Engineers & System Integrators](#5-role-4-hardware-engineers--system-integrators)
   - [5.1 Bus Architecture: Dedicated Hardware vs Driver Mobile Separation](#51-bus-architecture-dedicated-hardware-vs-driver-mobile-separation)
   - [5.2 Dedicated Bus Door Ethernet/PoE RFID Reader Specification](#52-dedicated-bus-door-ethernetpoe-rfid-reader-specification)
   - [5.3 Dedicated Vehicle GPS Ingestion Protocol](#53-dedicated-vehicle-gps-ingestion-protocol)
   - [5.4 Offline Cellular Store-and-Forward Batch Sync](#54-offline-cellular-store-and-forward-batch-sync)
   - [5.5 Double-Tap Deduplication & Rogue Badge Security Audits](#55-double-tap-deduplication--rogue-badge-security-audits)
   - [5.6 CLI Hardware Simulator Operations](#56-cli-hardware-simulator-operations)
6. [System Administration & Troubleshooting](#6-system-administration--troubleshooting)
   - [6.1 Health Check Endpoints](#61-health-check-endpoints)
   - [6.2 Rate Limiting Policies](#62-rate-limiting-policies)
   - [6.3 Security Headers & WebSockets](#63-security-headers--websockets)
   - [6.4 Diagnostics FAQ](#64-diagnostics-faq)

---

## 1. Platform Overview & Design Architecture

SchoolTrack is engineered on **.NET 10 (LTS)** and **PostgreSQL 17**, structured as a Clean Architecture Modular Monolith with zero third-party JavaScript framework dependencies. The platform guarantees:
- **Zero Driver Dependency for Safety Tracking**: Physical vehicle location tracking and student RFID tap ingestion are executed strictly by vehicle-mounted hardware. If a driver forgets their mobile phone, runs out of battery, or loses cellular connectivity, the bus's dedicated GPS tracker and bus door Ethernet RFID scanner communicate directly with `SchoolTrack.DeviceGateway` over secure cellular APN.
- **Strict Multi-Tenant Isolation**: Tenant boundaries (`SchoolId`) are enforced at the database layer via EF Core Global Query Filters. School administrators, drivers, and parents have zero visibility into any other institution's data.
- **Claymorphic Stitch UI System**: Tactile, high-visibility user interfaces featuring rounded pill badges, multi-layered soft drop shadows, safety mint `#0C6B55` primary actions, transit yellow `#FFD66B` alerts, and coral red `#BA1A1A` emergency signals.

```
                      +---------------------------------------+
                      |       DEDICATED BUS HARDWARE          |
                      |  [GPS Tracker]     [Door RFID Reader] |
                      +-------+-------------------+-----------+
                              | Cellular APN      | PoE/Cellular
                              v                   v
+-------------------+ +---------------------------------------+ +--------------------+
|  Blazor Admin Web | |       SchoolTrack.DeviceGateway       | | Driver MAUI Mobile |
|  (Transport Mgrs) | |    (Telemetry Ingestion & Filtering)  | |  (Turn-by-Turn/HUD)|
+---------+---------+ +-------------------+-------------------+ +---------+----------+
          |                               |                                 |
          +-----------------------+       |       +-------------------------+
                                  v       v       v
                      +---------------------------------------+
                      |         ASP.NET Core Web API          |
                      |     (SignalR Tracking Hub Engine)     |
                      +-------------------+-------------------+
                                          |
                        +-----------------+-----------------+
                        |                                   |
                        v                                   v
             +---------------------+             +--------------------+
             |    PostgreSQL 17    |             | Parent MAUI Mobile |
             | (EF Core Multi-Ten) |             | (Live Bus Radar)   |
             +---------------------+             +--------------------+
```

---

## 2. Role 1: School Administrators & Transport Directors

The **SchoolTrack Admin Web Application** (`src/SchoolTrack.Admin`) provides central transportation oversight, fleet logistics, student assignment, and real-time operations radar.

### 2.1 Authentication & Multi-Tenant Setup
1. Open the Admin Web portal URL (e.g., `https://admin.schooltrack.internal` or `http://localhost:5200`).
2. Enter your registered administrative credentials:
   - **School Admin Email**: `admin@springfield.edu`
   - **Password**: Provided during institution onboarding.
3. Upon sign-in, the system assigns a cryptographically signed JWT token embedding your unique `school_id`. All subsequent requests automatically apply multi-tenant isolation filters.

### 2.2 Fleet Management (Buses & Driver Rotation)
- **Adding a Bus**:
  1. Navigate to **Fleet Management** -> **Buses** (`/fleet/buses`).
  2. Click **+ Add Vehicle**.
  3. Enter the Bus Registration Number (e.g., `BUS-104`), Internal Fleet Number (e.g., `14`), Seating Capacity (e.g., `48`), and Model/Chassis details.
  4. Submit to create the vehicle in the active fleet registry.
- **Assigning a Driver**:
  1. In the Bus Details view, click **Assign Driver**.
  2. Select the certified driver from the active dropdown (e.g., `Robert Jenkins (CDL: CDL-99281-IL)`).
  3. Set the assignment start date. Any previous active assignment for that bus is automatically archived with an end date to prevent scheduling collisions.

### 2.3 Route & Stop Configuration
- **Creating a Route**:
  1. Navigate to **Routes** (`/routes`).
  2. Click **Create New Route**.
  3. Provide Route Name (e.g., `Route 14 — North Hills Morning Run`), Shift Type (`Morning AM` or `Afternoon PM`), and Default Departure Time (e.g., `07:15 AM`).
- **Sequencing Route Stops**:
  1. Within the Route Planner, click **+ Add Waypoint Stop**.
  2. Assign Stop Name (e.g., `Pine & 5th Ave`), GPS Coordinates (`Latitude`, `Longitude`), Scheduled Arrival Time, and Geofence Radius (default: `100 meters`).
  3. Reorder stops using the sequence index numbers. The system calculates expected segment transit times.

### 2.4 Student & Guardian Enrollment
- **Enrolling a Student**:
  1. Navigate to **Students** (`/students`).
  2. Click **+ Register Student**.
  3. Enter First Name, Last Name, Admission Number (e.g., `ADM-88219`), Class/Grade (e.g., `Grade 4-B`), Morning Pickup Stop, and Afternoon Dropoff Stop.
- **Linking Guardians**:
  1. Under the Student Profile, navigate to the **Guardians** section and click **Link Guardian**.
  2. Select or invite the parent account by email (e.g., `elena.martinez@gmail.com`).
  3. Specify the Relationship (`Mother`, `Father`, `Legal Guardian`) and mark whether the contact holds Emergency Pickup Authorization.

### 2.5 RFID Badge Provisioning & Hardware Device Registration
- **Issuing Student RFID Badges**:
  1. Navigate to **Hardware & Devices** -> **RFID Cards** (`/devices/rfid-cards`).
  2. Click **Issue Badge**.
  3. Enter the physical 8-to-14 character hexadecimal Card UID (e.g., `E28011606000020473918451`).
  4. Link the badge to the target student. A student can only have one active card at any given time.
- **Registering Bus Door Scanners & Vehicle GPS Units**:
  1. Navigate to **Hardware Devices** (`/devices/hardware`).
  2. To link a vehicle GPS unit: Select **Register GPS Device**, input the device IMEI (15 digits), and assign it to the bus (e.g., `Bus #14`).
  3. To link a door scanner: Select **Register RFID Reader**, input the Ethernet MAC address / Hardware Device ID (e.g., `RFID-DOOR-BUS14`), and assign it to the entrance door.

### 2.6 Real-Time Live Fleet Radar
- Navigate to **Live Tracking Radar** (`/radar`).
- **Interactive Map**: Displays all active buses currently executing morning or afternoon trips with real-time GPS coordinates, vehicle heading arrows, speed gauges, and route paths.
- **Status Filter Controls**:
  - Filter by status: `In Transit`, `Approaching Stop`, `Boarding Active`, `Delayed`, `Emergency Alert`.
- **Live Boarding Feed**: Right-hand panel streams door scanner taps as they happen with student names, timestamps, and seat numbers.

### 2.7 Audit Logs, Safety Alerts & Compliance Reporting
- **Emergency Incident Review**: Any in-cab SOS triggered by a driver immediately generates a high-priority red alert banner across all active admin sessions with audible chime.
- **Rogue Card Security Audit**: Unregistered RFID cards scanned at the bus door are rejected by the reader, logged in the `UnknownCardTap` audit table with GPS coordinates and timestamp, and flagged for security review.
- **Compliance Export**: Download end-of-month boarding manifests, on-time performance metrics, and pre-trip vehicle safety logs in CSV/PDF format.

---

## 3. Role 2: School Bus Drivers (In-Cab Console)

The **SchoolTrack Driver Application** (`src/SchoolTrack.DriverApp`) is a specialized .NET MAUI Blazor Hybrid application optimized for ruggedized 8"–10" dashboard tablets and mobile viewports.

### 3.1 In-Cab Driver Sign-In & Verification
1. Dock tablet into vehicle dashboard mount and launch **SchoolTrack Driver**.
2. Enter your Commercial Driver's License number (CDL) and secure 4-digit PIN (or use the one-tap Quick Demo Login for `Robert Jenkins`).
3. The app authenticates with the central API, loads your assigned vehicle (`Bus #14`), active shift, and morning route stops.

### 3.2 Pre-Trip CDL Safety Inspection Checklist
Before moving the vehicle, transport regulations mandate completing the vehicle pre-trip safety checklist:
1. Tap the **Safety** tab on the bottom navigation bar.
2. Review vehicle telemetry: Fuel Level (e.g., `82% Diesel`), Tire Pressure (`102 PSI Nominal`), Odometer (`42,180 mi`).
3. Complete the 6-point tactile inspection checklist:
   - [x] Hydraulic / Air Brakes & Emergency Brake Test
   - [x] Front, Rear, Hazard & Warning Flashers
   - [x] Tire Tread Depth & Lug Nut Integrity
   - [x] Emergency Exits & Roof Hatch Clearances
   - [x] First Aid Kit & Charged Fire Extinguisher Present
   - [x] Passenger Seat Belts & Aisle Clearances Inspected
4. Tap **Sign & Certify Inspection**.
5. Tap **Start Morning Trip**. The SignalR tracking engine broadcasts the `TripStarted` event to central dispatch and parent apps.

### 3.3 Live In-Cab Turn-by-Turn Navigation & HUD
The **Route** screen (`/navigation`) is the driver's primary heads-up display:
- **Turn-by-Turn Instruction Card**: Displays the immediate next navigation maneuver (e.g., *"In 300 Feet • Turn Left on Evergreen Blvd"*).
- **Speed & Limit HUD**: Displays live GPS vehicle speed (e.g., `22 MPH`) alongside the local school zone speed limit indicator (`Limit 25`).
- **Leaflet Navigation Canvas**: Shows the active bus icon with live bearing rotation along the planned route polyline.
- **Next Stop Card**: Highlights the upcoming stop name, estimated arrival time, and student count scheduled for curbside pickup.
- **Arrival Action Button**: Upon reaching the curbside waypoint, tap the large mint button:
  `Arrived at Stop 4 • Chime Door Scanner`.
  This signals the door RFID scanner to activate the boarding countdown tone.

### 3.4 Student Boarding Roster & Door RFID Synchronization
1. Tap the **Roster** tab on the bottom navigation bar (`/roster`).
2. The top hero banner displays the status of the dedicated entrance door scanner:
   `Reader #01 Online • 24/26 Boarded (92%)`.
3. When students step onto the bus and tap their RFID card against the entrance reader:
   - The reader sounds an affirmative beep and flashes green.
   - The in-cab tablet displays an instant toast notification:
     *"Verified Tap: Leo Martinez (Seat 4A) • Boarded Stop #4"*.
   - The student's status automatically transitions from `Pending` to `Boarded` with a green checkmark.

### 3.5 Manual Check-In Overrides & Absence Logging
In the event a student forgets or misplaces their RFID card:
1. Locate the student in the Roster manifest list.
2. Tap the tactile **Manual Check-In** button.
3. Confirm the student is visually present. The system records a verified manual check-in with the driver's user ID appended for audit accountability.
4. If a parent reported their child absent earlier in the morning, an orange badge appears:
   `Excused Absence: Doctor Appointment`.

### 3.6 In-Cab Delay Broadcast System
If encountering heavy morning traffic, severe weather, or a mechanical issue:
1. Tap the **Delay** tab (`/broadcast`) or the quick delay icon on the HUD.
2. Select the delay increment pill: `+5m`, `+10m`, `+15m`, or `+20m`.
3. Select the reason tag chip: `Heavy Traffic`, `Construction`, `Weather`, `Accident`, or `Mechanical`.
4. Enter optional driver notes (e.g., *"Main St water main break detour via Elm"*).
5. Toggle target recipients: `Parents on Route` (enabled by default) and `Central Dispatch`.
6. Tap **Broadcast Delay Alert**. Push notifications are immediately dispatched to all affected parents, and ETAs automatically recalculate.

### 3.7 Emergency SOS Incident Protocol
If an immediate medical emergency, accident, or security incident occurs:
1. Tap the prominent red **SOS Emergency** button on the top HUD bar.
2. A safety confirmation modal appears: *"Dispatch Emergency Incident Alert?"*.
3. Tap **Confirm 911 SOS Alert**.
4. The system immediately:
   - Flashes an emergency priority alert to Central School Dispatch with live GPS coordinates.
   - Pushes incident notifications to transport supervisors.
   - Locks the vehicle's telemetry stream into high-frequency emergency tracking mode (1-second GPS intervals).

### 3.8 Shift Completion & Vehicle Sign-Off
1. Once all students are dropped off at school (AM) or home stops (PM), navigate to the **Safety** screen.
2. Walk the aisle to perform the mandatory **Child Safety Check** (confirming no sleeping children remain onboard).
3. Tap **Complete Route & End Shift**. The trip status transitions to `Completed`, and all tracking channels safely close.

---

## 4. Role 3: Parents & Guardians (Parent App)

The **SchoolTrack Parent Application** (`src/SchoolTrack.ParentApp`) is a .NET MAUI Blazor Hybrid mobile app engineered for iOS and Android, implementing the tactile Claymorphic Stitch UI design tokens.

### 4.1 Secure Mobile Authentication & Biometrics
1. Download and open **SchoolTrack Parent**.
2. Sign in with your registered parent email and password (or use one-tap demo credentials for `elena.martinez@gmail.com`).
3. Enable biometric login (Face ID / Fingerprint) or 4-digit PIN for rapid authentication.

### 4.2 Multi-Child Profile Switcher
If you have multiple children attending the school district:
- Tap the top child switcher pill (e.g., `Leo Martinez (Grade 4-B)` vs `Mia Martinez (Grade 1-A)`).
- The dashboard immediately adapts to display the selected student's assigned bus, morning/afternoon route, and current status.

### 4.3 Live Bus Radar & Predictive Dynamic ETA
The Home Dashboard displays the **Live Bus Radar** card:
- **Status Badge**: `Bus In Transit`, `Approaching Your Stop`, `Boarded`, or `At School`.
- **Dynamic ETA Countdown**: Large lavender clay badge displaying real-time arrival estimates (e.g., `8 mins • 07:42 AM`).
- **Bus Speed & Distance**: Live speed readout (e.g., `22 MPH`) and linear distance to your pickup stop (e.g., `1.2 miles away`).

### 4.4 Interactive Route & Vehicle Map
1. Tap the **Map** tab on the bottom bar (`/map`).
2. An interactive Leaflet map displays:
   - High-visibility safety teal route polyline.
   - Animated bus marker with directional heading rotation pointing in the exact direction of travel.
   - Your child's designated bus stop pin (`Pine & 5th Ave`).
   - Destination school marker (`Springfield Elementary`).
3. Tap **Recenter Bus** at any time to lock the viewport onto the moving vehicle.

### 4.5 Real-Time Boarding & Deboarding Push Timeline
Parents receive instant push notifications as their child transitions along their journey:
- `07:15 AM`: *"Bus #14 has started the Morning Route."*
- `07:38 AM`: *"Bus #14 is approaching Pine & 5th Ave (ETA: 4 minutes)."*
- `07:43 AM`: *"Leo Martinez scanned onto Bus #14 at Pine & 5th Ave (Seat 4A)."*
- `08:05 AM`: *"Bus #14 safely arrived at Springfield Elementary. Leo scanned off."*

### 4.6 Student RFID Scan History Verification
1. Tap the **Activity** tab (`/activity`).
2. Review the chronological audit log of all card taps, complete with:
   - Date and precise timestamp.
   - Event type (`Boarded Bus`, `Deboarded Bus`).
   - Stop location name and verified door reader ID (`RFID-DOOR-BUS14`).

### 4.7 Absence Submission & Driver Notice
If your child will not ride the bus on a given day:
1. Navigate to **Report Absence** in the Parent App.
2. Select the Date, Shift (`Morning AM`, `Afternoon PM`, or `All Day`), and Reason (`Illness`, `Doctor Appointment`, `Family Vacation`, `Parent Dropoff`).
3. Add an optional note to the driver.
4. Tap **Submit Notice**. The driver's manifest roster updates instantly with an orange excused badge, preventing unnecessary dwell time at your stop.

### 4.8 Direct Driver & School Dispatch Communication
- Tap the **Call Dispatch** button to connect directly with the central transport office.
- Tap **Message Transport Office** for non-urgent questions regarding route scheduling or badge replacements.

---

## 5. Role 4: Hardware Engineers & System Integrators

### 5.1 Bus Architecture: Dedicated Hardware vs Driver Mobile Separation
A core principle of SchoolTrack is the complete physical separation between in-cab driver UI and life-safety hardware telemetry:
- **Dedicated Vehicle GPS Tracker**: Ruggedized unit wired directly to the vehicle's 12V/24V ignition power and CAN bus, equipped with internal backup battery and private cellular APN SIM card.
- **Dedicated Bus Door RFID Reader**: Industrial PoE/Ethernet reader installed directly above/beside the passenger boarding entrance, scanning 13.56 MHz Mifare / ISO 14443A student cards.
- **Zero Driver Dependency**: Telemetry streams directly from the hardware devices to `SchoolTrack.DeviceGateway`. A driver's personal or dashboard tablet failure has zero impact on vehicle tracking or student boarding safety.

### 5.2 Dedicated Bus Door Ethernet/PoE RFID Reader Specification
- **Network Interface**: 10/100 Ethernet RJ45 with 802.3af PoE or 12V DC terminal.
- **Supported Standards**: ISO/IEC 14443 Type A/B, MIFARE Classic 1K/4K, MIFARE Ultralight, NTAG.
- **Ingestion Payload (HTTP POST / TCP Socket to Gateway)**:
  `POST /api/v1/devices/rfid/scan`
  ```json
  {
    "deviceSerialNumber": "RFID-DOOR-BUS14",
    "cardUid": "E28011606000020473918451",
    "scanTimestamp": "2026-11-14T07:42:15Z",
    "readerLocation": "FrontDoor",
    "busId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
  }
  ```

### 5.3 Dedicated Vehicle GPS Ingestion Protocol
- **Ingestion Payload (HTTP POST / TCP Socket to Gateway)**:
  `POST /api/v1/devices/gps/telemetry`
  ```json
  {
    "deviceImei": "860492040192831",
    "latitude": 39.7817,
    "longitude": -89.6501,
    "speed": 22.4,
    "heading": 184.2,
    "altitude": 182.5,
    "accuracy": 1.2,
    "hdop": 0.9,
    "satelliteCount": 11,
    "ignitionOn": true,
    "recordedAt": "2026-11-14T07:42:15Z"
  }
  ```

### 5.4 Offline Cellular Store-and-Forward Batch Sync
When the vehicle navigates through cellular dead zones or underground passes:
1. The onboard GPS unit and RFID reader buffer unsent telemetry records into non-volatile flash memory (FIFO queue).
2. Upon cellular reconnection, the hardware submits a bulk batch sync payload:
   - GPS Batch: `POST /api/v1/devices/gps/batch`
   - RFID Batch: `POST /api/v1/devices/rfid/batch`
3. The gateway ingests all backlogged records, applies correct historical timestamps, updates route progress retrospectively, and synchronizes the SignalR tracking state.

### 5.5 Double-Tap Deduplication & Rogue Badge Security Audits
- **15-Second Deduplication Cooldown**: If an excited student taps their RFID badge multiple times in rapid succession, the backend identifies the duplicate UID within a 15-second sliding window, records the tap as deduplicated, and prevents multiple boarding notifications.
- **Rogue Badge Handling**: If an unknown card UID (e.g., student's gym locker card or stranger's badge) is tapped:
  1. The door reader generates a distinct error buzz tone.
  2. The gateway records the event in the `UnknownCardTap` security audit log with reader ID, GPS coordinates, and timestamp.
  3. The system remains fully operational without crashing or blocking subsequent valid taps.

### 5.6 CLI Hardware Simulator Operations
For development, staging verification, and demonstration environments, the solution includes a dedicated CLI simulator:
- **Executable**: `tools/SchoolTrack.HardwareSimulator`
- **Execution Syntax**:
  ```powershell
  # Interactive Terminal Menu
  dotnet run --project tools/SchoolTrack.HardwareSimulator

  # Automated Mode: Continuous Kinematic GPS Simulation
  dotnet run --project tools/SchoolTrack.HardwareSimulator -- --mode gps --bus BUS-104

  # Automated Mode: Curbside Boarding Sweep (RFID)
  dotnet run --project tools/SchoolTrack.HardwareSimulator -- --mode rfid --bus BUS-104

  # Automated Mode: Dead-Zone Cellular Disruption Batch Ingestion
  dotnet run --project tools/SchoolTrack.HardwareSimulator -- --mode batch --bus BUS-104

  # Automated Mode: Full End-to-End Golden Trip Lifecycle
  dotnet run --project tools/SchoolTrack.HardwareSimulator -- --mode golden --bus BUS-104
  ```

---

## 6. System Administration & Troubleshooting

### 6.1 Health Check Endpoints
SchoolTrack provides standard ASP.NET Core health check probes for container orchestrators (Kubernetes / Docker Compose):
- `GET /health`: Overall system readiness and database connectivity.
- `GET /health/live`: Liveness probe (confirms process is responsive).
- `GET /health/ready`: Readiness probe (confirms database and cache can accept traffic).

### 6.2 Rate Limiting Policies
To safeguard ingestion gateways against hardware packet flooding or brute force attacks:
- `AuthPolicy`: 15 requests per minute per IP address.
- `HardwarePolicy`: 150 requests per minute per device serial/IMEI (supports high-frequency GPS telemetry).
- `GeneralPolicy`: Token bucket allowing 100 requests with 30-token burst replenishment.

### 6.3 Security Headers & WebSockets
All outgoing HTTP responses enforce enterprise transport security:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: SAMEORIGIN`
- `X-XSS-Protection: 1; mode=block`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Strict-Transport-Security: max-age=31536000; includeSubDomains`
- Reverse proxy (Nginx) must configure WebSocket headers (`Upgrade`, `Connection: Upgrade`) for `/hubs/tracking`.

### 6.4 Diagnostics FAQ
- **Q: Driver tablet shows "Disconnected from Tracking Hub"?**
  *A*: Verify driver tablet has cellular or Wi-Fi data enabled. SignalR automatically retries connection with exponential backoff.
- **Q: RFID tap shows in door scanner but not on driver tablet?**
  *A*: Verify the driver has joined the active trip. The tablet receives SignalR boarding events filtered by `bus_{busId}` and `trip_{tripId}`.
- **Q: Parent App map is not centering on bus?**
  *A*: Tap the **Recenter** button on the bottom right of the map canvas. Ensure location permissions are granted if tracking distance from parent location.

