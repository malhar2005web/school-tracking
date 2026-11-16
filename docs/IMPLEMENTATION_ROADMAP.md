# SchoolTrack Implementation Roadmap

This roadmap governs phased delivery. Each phase requires design/plan notes, compilation, automated tests, migration verification, and `docs/PHASE_PROGRESS.md` updates before advancing.

---

## Phase Overview

| Phase | Name | Primary Deliverable | Verification Gate |
|---|---|---|---|
| **Phase 0** | Project Analysis & Architecture | Repository analysis, Stitch UI inventory, Architecture, DB Design, Roadmap | Documentation complete & reviewed |
| **Phase 1** | Solution Foundation | `.NET 10` solution, EF Core + PostgreSQL, initial migration, Serilog, Swagger, Health checks, Docker Compose | Solution builds; API starts; DB connects; Swagger & `/health` work |
| **Phase 2** | Auth & Multi-School Foundation | Schools, ASP.NET Identity, RBAC policies, JWT + rotating refresh tokens, tenant isolation, audit logs | Auth tests pass; School A blocked from School B data |
| **Phase 3** | Core School Management | Classes, Students, Parents, Parent-Student links, Drivers, Buses, RFID Cards, GPS/RFID Device registration | CRUD + relationship validation + tenant isolation tests pass |
| **Phase 4** | Routes & Trip Engine | Routes, ordered RouteStops, assignments, AM/PM Trips, state machine (`Scheduled -> Completed`), history | Valid/invalid transitions & driver assignment scoping verified |
| **Phase 5** | GPS Gateway & Simulator | `IGpsProtocolAdapter`, device auth, coordinate/timestamp validation, `LocationLogs`, `LatestBusLocations`, heartbeat/offline detector, GPS simulator | Simulated route packets update DB & latest bus location; invalid packets rejected |
| **Phase 6** | RFID Gateway & Simulator | `IRfidProtocolAdapter`, device auth, `CardUid` resolution, active trip check, deduplication, unknown-card logging, RFID simulator | Simulated taps create boarding/drop-off events; duplicates & unknown cards handled safely |
| **Phase 7** | Real-Time Tracking (SignalR) | Authenticated `TrackingHub`, authorized school/trip/parent/driver groups, live GPS/RFID/trip/offline events | Authorized clients receive scoped events; cross-tenant/unauthorized subscriptions fail |
| **Phase 8** | Admin Web App (C# Blazor) | Claymorphic Stitch UI in `SchoolTrack.Admin` across all 7 nav sections + Login + live map & SignalR | Matches Stitch UI; full API integration; loading/error/empty states; form validation |
| **Phase 9** | Parent Mobile App (.NET MAUI Hybrid) | Parent login, linked children, dashboard, live map tracking, boarding/drop-off & FCM push notifications, trip history, child settings | Parent sees only linked children; live tracking & notification flows verified |
| **Phase 10** | Driver Mobile App (.NET MAUI Hybrid) | Driver login, assigned bus/route/trips, start/complete trip, stop manifest & manual overrides, delay & emergency SOS reporting | Driver scoped to assignments; trip lifecycle & audit trail verified |
| **Phase 11** | Physical Hardware Integration | Real vendor protocol adapters (`TcpRfidAdapter`, `VendorGpsAdapter`, etc.) once specs/packets arrive | Tested with captured hardware packets & physical devices |
| **Phase 12** | Production Readiness | Security audit, indexing/performance review, backup & data retention jobs, HTTPS/Docker/deployment guide | Full test suite passes; end-to-end workflows & deployment verified |

---

## Key Technical Decisions & Mitigations

1. **Map Provider Abstraction**: Implement `IMapProvider` on the frontend/shared layer using **OpenStreetMap + Leaflet** (no billing lock-in or hardcoded keys required in dev/CI) with clean configuration hooks to switch tile/SDK providers (Mapbox or Google Maps) via environment configuration.
2. **Hardware Protocol Uncertainty**: Physical Ethernet RFID reader and GPS tracker vendors are unknown. All ingestion flows through `SchoolTrack.DeviceGateway` (`IRfidProtocolAdapter` / `IGpsProtocolAdapter`) with an HTTP/JSON reference simulator adapter for development and testing. No vendor protocol will be fabricated or assumed.
3. **Stitch Image Assets**: External `lh3.googleusercontent.com` links in Stitch HTML may expire over time. During Phase 8–10, local clay-style avatars/Ollie mascot assets will be stored in `wwwroot/images/` alongside compiled CSS tokens (`Plus Jakarta Sans` + `Material Symbols Outlined`).
4. **Driver Phone vs Bus Hardware**: Driver MAUI app will NEVER act as the primary GPS source. It controls trip lifecycle, stop arrival/departure triggers, manual manifest overrides, and emergency/delay alerts.
5. **File Size & Modularity Guardrail**: Keep controllers thin, entity configurations per-file, validators separate, and split any growing service/component well before reaching 500 lines.

