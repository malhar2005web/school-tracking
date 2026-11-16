# Stitch UI Analysis & Frontend Architecture Plan

This document inventories and analyzes the supplied Stitch UI package (`stitch_schooltrack_parent_app_ui.zip`, preserved in `design/stitch-ui/`).

---

## 1. Design System: "Claymorphic Safety & Transit"

Extracted from `design/stitch-ui/claymorphic_safety_transit/DESIGN.md` and verified across all `code.html` files:

### Typography
- **Font Family**: `Plus Jakarta Sans` (Weights: `500`, `600`, `700`, `800`)
- **Iconography**: Google `Material Symbols Outlined` (`opsz 20..48`, `wght 100..700`, `FILL 0..1`)
- **Scale**:
  - `headline-xl`: 34px / 42px / 800 (`-0.02em`) — desktop KPIs & page titles
  - `headline-xl-mobile`: 28px / 36px / 800 (`-0.02em`)
  - `headline-lg`: 26px / 34px / 700 (`-0.015em`)
  - `headline-md`: 22px / 28px / 700 (`-0.01em`)
  - `headline-sm`: 18px / 24px / 700
  - `body-lg`: 16px / 24px / 500
  - `body-md`: 14px / 20px / 500
  - `body-sm`: 12px / 16px / 500
  - `label-lg`: 15px / 20px / 700
  - `label-md`: 13px / 18px / 700
  - `label-sm`: 11px / 14px / 700

### Color Tokens
- **Canvas / Surface**: `#fbf8ff` (`surface` / `background`), `#f5f2ff` (`surface-container-low`), `#eeecff` (`surface-container`), `#e7e6ff` (`surface-container-high`), `#e1e0ff` (`surface-container-highest`), `#ffffff` (`surface-container-lowest`)
- **Text / Neutrals**: `#181930` (`on-surface`), `#484552` (`on-surface-variant`), `#797584` (`outline`), `#cac4d4` (`outline-variant`), `#2d2e47` (`inverse-surface`)
- **Primary (Soft Lavender / Indigo)**: `#644bba` (`primary`), `#9b83f6` (`primary-container`), `#e7deff` (`primary-fixed`), `#310b87` (`on-primary-container`)
- **Secondary (Safety Mint)**: `#0c6b55` (`secondary`), `#a1f3d7` (`secondary-container` / `secondary-fixed`), `#85d6bb` (`secondary-fixed-dim`), `#18715b` (`on-secondary-container`)
- **Tertiary (Transit Amber/Yellow)**: `#765b00` (`tertiary`), `#cca741` (`tertiary-container`), `#ffdf93` (`tertiary-fixed`), `#e9c259` (`tertiary-fixed-dim`)
- **Alert / Error (Coral Red)**: `#ba1a1a` (`error`), `#ffdad6` (`error-container`), `#93000a` (`on-error-container`)

### Tactile Claymorphism Physics & Shapes
- **Corner Radii**: Cards/panels `1rem`–`2rem` (16px–32px); pills/chips/buttons `9999px` (`rounded-full`); floating nav bars `3rem` (`rounded-xl`). Sharp corners are avoided.
- **Clay Elevation**:
  - **Cards (Level 1)**: Solid `#ffffff` with dual ambient drop shadow + subtle inner top-left highlight & bottom-right depth shadow (`inset 2px 2px 4px rgba(255,255,255,0.9), inset -2px -3px 4px rgba(41,42,66,0.04)`).
  - **Interactive Pills / Buttons (Level 2)**: Colored clay surface with top specular rim (`inset 0 3px 2px rgba(255,255,255,0.45)`), bottom physical lip (`inset 0 -4px 0 rgba(...)`), and active press compression (`translateY(3px)`).
  - **Inset Inputs & Wells**: Recessed `#f5f2ff` surfaces with concave inner shadow (`inset 2px 2px 4px rgba(41,42,66,0.08)`).
  - **Modals / Drawers (Level 3)**: Blurred backdrop (`rgba(41,42,66,0.35)` + `backdrop-blur-[6px]`).

---

## 2. Screen Inventory (from `design/stitch-ui/`)

| Folder | Target App | Shell Type | PNG? | Screen Purpose & Key Elements |
|---|---|---|---|---|
| `schooltrack_admin_fleet_live_radar` | **Admin Web (Blazor)** | `web_dashboard` | Yes | Primary Fleet Radar dashboard: fixed 72-width sidebar, top district/gateway/active-bus/SOS/user header, 4 KPI bento cards, interactive map radar with fleet filter pills (`All Fleet`, `Morning Inbound`, `At School Gates`, `Delayed`), selected bus inspector card + stop sequence timeline, right-rail `RFID Live Stream` & `Active Dispatch` broadcaster. |
| `schooltrack_admin_student_rfid_roster_logs` | **Admin Web (Blazor)** | `web_dashboard` | Yes | Student RFID Transit Roster & Logs: 5 summary KPI tiles, search + Grade/Route/Tap-Status/Date filters, sortable/clickable manifest table with student avatars/RFID UID/bus/scan timestamps/guardian contact actions, slide-out `Live Scan Inspection` right drawer with mini-map & RSSI telemetry, and `+ Issue Temp RFID Pass` modal. |
| `schooltrack_welcome_login` | **Parent Mobile (MAUI)** | `mobile_blank` | Yes | Welcome & Parent Sign-In: Spring term & certified badges, interactive Ollie the Owl mascot stage with speech bubble, 3 clay feature strip cards (GPS, RFID, SOS), inset phone number login card, and OTP verification card. |
| `schooltrack_parent_app` | **Parent Mobile (MAUI)** | `mobile_blank` | No | Exact duplicate of `schooltrack_welcome_login/code.html` (identical SHA-256 hash). |
| `schooltrack_parent_dashboard` | **Parent Mobile (MAUI)** | `mobile_tab` | Yes | Parent Home Dashboard: greeting + SOS trigger pill, `Transit Radar • Live Map` mini-map preview card with `View Full Live Map` CTA, active child transit hero card (RFID verified pill, ETA amber banner, driver mini-card with call/chat), 2x2 Quick Safety Controls (`Live Route`, `Tap Logs`, `Skip Ride`, `Direct Chat`), Ollie assistant widget, 4-tab floating bottom nav. |
| `schooltrack_live_bus_tracking` | **Parent Mobile (MAUI)** | `mobile_tab` | Yes | Full Live Map Tracker: bus #14 speed/weather/segmented stop progress card, interactive route map canvas with completed/upcoming path, pulsing bus marker, pickup waypoint badge, re-center/layer controls, bottom reassurance sheet (ETA hero, seat/RFID status, safe driving status, Call Driver modal, School Dispatch, Share Live Journey). |
| `schooltrack_rfid_trip_history` | **Parent Mobile (MAUI)** | `mobile_tab` | No | Transit History & RFID Ledger: sculpted student smart-card header, date filter pills (`Today`, `Yesterday`, `Past 7 Days`), `Live Transit Milestones` vertical timeline with geotag map thumbnail, Ollie streak achievement card, and past trip history cards with PDF download trigger. |
| `schooltrack_child_profile_settings` | **Parent Mobile (MAUI)** | `mobile_tab` | Yes | Child Profile & Parent Account Settings: active child switcher pill, student identity card + RFID tag status, route & morning/afternoon schedules + driver contact, 4 clay toggle switches (`Transit Safety Alerts`), `Authorized Guardians` list + temporary pickup CTA, and `School Transport Helpdesk` + SOS panic alert toast. |
| `schooltrack_driver_active_route_navigation` | **Driver Mobile (MAUI)** | `mobile_tab` | Yes | Driver Route & Navigation Console: Bus #14 header with GPS/RFID hardware status pills and SOS button, route/RFID/accuracy status bar, purple turn-by-turn instruction & speed HUD card, map viewport with route & stop markers, Next Stop card with student initials cluster, 3 passenger counters (`Onboard`, `To Pick Up`, `School Target`), large tactile `Arrived at Stop`, `Delay Alert`, and `Radio Base` buttons, 4-tab bottom nav. |
| `schooltrack_driver_rfid_student_roster_tap_log` | **Driver Mobile (MAUI)** | `mobile_tab` | Yes | Driver RFID Roster & Tap Log: hardware reader online banner (`24/26 Boarded` progress), green `Live Tap Registered` highlight card, filter pills (`Current Stop`, `All Students`, `Boarded`, `Missing/Pending`), stop-grouped student cards with `Manual Check-in` / `Mark Absent` overrides, parent-excused `Not Riding` notice banner, `Complete Boarding for Stop` & `Manual Student Check-in / Lost Card` CTAs. |

---

## 3. Navigation Structure

### Admin Web (`SchoolTrack.Admin` — Blazor)
Persistent left sidebar (`w-72`) + fixed top header (`h-20`):
1. `Fleet Live Radar` (`/admin/radar` — Dashboard & Live GPS Tracking)
2. `Student RFID Roster & Logs` (`/admin/students` — Students, RFID Cards & Attendance Logs)
3. `Route Management` (`/admin/routes` — Routes, Stops, Schedules & Trip Management)
4. `Incident Alerts` (`/admin/alerts` — SOS, Delays, Unknown RFID Cards & Offline Devices)
5. `Driver Directory` (`/admin/drivers` — Drivers, Buses & Hardware Device Mapping)
6. `Reports & Compliance` (`/admin/reports` — Attendance, Trip, Route & Device Health Reports)
7. `Settings` (`/admin/settings` — School Settings, Users, Roles, Notifications & Audit Logs)

*(Note: Parent Management, Bus Management, Trip Creation/History, and Device Credentials/Protocol Config are integrated cleanly within these 7 top-level navigation modules or sub-tabs/routes so every required Admin feature in Section 7A is reachable without breaking the Stitch sidebar hierarchy.)*

### Parent Mobile (`SchoolTrack.Mobile` — Parent Role)
Floating 4-tab bottom bar:
1. `Home` (`ParentDashboard`)
2. `Tracking` (`LiveBusTracking`)
3. `History` (`RfidTripHistory`)
4. `Profile` (`ChildProfileSettings`)
*(Plus pre-auth `WelcomeLogin` and modal/sub-pages for Notification History, Skip Ride, and Child Switcher).*

### Driver Mobile (`SchoolTrack.Mobile` — Driver Role)
Fixed 4-tab bottom bar:
1. `Route` (`DriverActiveRoute`)
2. `Roster` (`DriverRfidRoster`)
3. `Broadcast` (`DriverBroadcastDelay` — delay & emergency reporting)
4. `Safety` (`DriverSafetyProfile` — assigned bus/route, daily trips, start/complete trip, dispatch contact)
*(Plus pre-auth Driver Login screen matching the Stitch claymorphic login style).*

---

## 4. Assets & Shared Components

### External Assets Referenced in Stitch HTML
- **Google Fonts**: `Plus Jakarta Sans` (400, 500, 600, 700, 800) & `Material Symbols Outlined`.
  - *Action for Phase 8/9/10*: Bundle local `.woff2` font files in `wwwroot/fonts/` as offline/MAUI fallback alongside clean CSS tokens.
- **Images**: All current `<img>` and map-thumbnail URLs in the Stitch HTML point to `https://lh3.googleusercontent.com/aida-public/...` (Stitch-generated clay avatars, Ollie the Owl mascot, and map preview images).
  - *Action*: Download/cache or replace with local bundled assets in `wwwroot/images/` (using safe fallback avatars/Ollie SVG if any remote temporary URL expires) while preserving the exact visual aesthetic.
- **Tailwind CSS**: Stitch HTML uses Tailwind CDN (`cdn.tailwindcss.com`) with an inline `tailwind.config` defining the exact design tokens.
  - *Action*: Compile/translate all design tokens into structured CSS custom properties (`clay-tokens.css` + `clay-components.css`) and/or build-time Tailwind CSS so there is zero runtime CDN dependency in production or offline MAUI Hybrid apps.

### Reusable Razor Component Mapping
- `ClayCard.razor` (Level 1 elevated clay surface with inner bevel highlights)
- `ClayButton.razor` (Primary lavender, Secondary mint, Alert coral, Surface puck with tactile press state)
- `ClayBadge.razor` (Status pills: On Route, Boarded, Pending, Delayed, Offline)
- `ClayToggle.razor` (Recessed clay track + rolling thumb switch)
- `ClayMetricTile.razor` (KPI card with icon puck, value, subtitle, progress bar)
- `ClayModal.razor` (Level 3 blurred-backdrop clay sheet/dialog)
- `ClayToast.razor` (Floating notification capsule)
- `AdminSidebar.razor` & `AdminTopBar.razor` (Shared Blazor Admin shell)
- `MobileTopBar.razor` & `MobileBottomNav.razor` (Shared MAUI Blazor Hybrid shell)
- `MapCanvasComponent.razor` (Provider-agnostic map wrapper supporting Leaflet/OpenStreetMap, Mapbox, or Google Maps via JS interop)

---

## 5. Missing Screens (To Build Consistently in Claymorphic Style)

1. **Admin Login & School Context Switcher** (using `schooltrack_welcome_login` clay card aesthetic adapted for desktop Admin email/password login + MFA/refresh support).
2. **Admin Route & Stop Builder + Trip Scheduler** (`Route Management` route — map stop placement, stop reordering, AM/PM trip creation, driver/bus assignment).
3. **Admin Bus, Driver & Hardware Device Registry** (`Driver Directory` & Device sub-views — bus CRUD, driver CRUD, GPS/RFID device provisioning, credential generation/rotation, protocol config, heartbeat logs).
4. **Admin Parent & Student Management Forms** (linking multiple children to parents, assigning RFID cards, transport route/stop mapping).
5. **Admin Incident Alerts, Reports & School Settings** (unknown RFID cards, duplicate scan alerts, offline device alerts, attendance/utilization reports, user/role management, audit log viewer).
6. **Parent Notification Center & Multi-Child Switcher Modal** (notification history list, unread badges, child selector).
7. **Driver Login, Trip List (Start/Complete), Delay & Emergency Broadcast, and Safety Checklist/Profile** (screens linked from the Driver bottom nav tabs `Broadcast` and `Safety`).

