# SchoolTrack — Production Deployment & Infrastructure Guide

## 1. System Overview & Architecture Topology

SchoolTrack is an enterprise-grade multi-tenant student safety and school transportation management platform.

```
                   ┌─────────────────────────────────────────────────────────┐
                   │                     CLIENTS / APPS                      │
                   │  • Blazor Admin Web Portal (Port 5081)                 │
                   │  • Parent Mobile App (.NET MAUI Blazor Hybrid)          │
                   │  • Driver In-Cab Console (.NET MAUI Blazor Hybrid)      │
                   └────────────────────────────┬────────────────────────────┘
                                                │ HTTPS / WSS
                                                ▼
                   ┌─────────────────────────────────────────────────────────┐
                   │                   REVERSE PROXY / TLS                   │
                   │      Nginx / Caddy / Traefik / AWS ALB / Cloudflare     │
                   └────────────────────────────┬────────────────────────────┘
                                                │ HTTP / WebSocket (Port 5080)
                                                ▼
┌─────────────────────────────────┐   ┌─────────────────────────────────────┐
│       HARDWARE SENSORS          │   │        ASP.NET CORE 10 WEB API      │
│ • Dedicated Bus GPS Trackers    ├──►│ • Device Gateway (GPS & RFID)       │
│ • Bus Door Ethernet RFID Readers│   │ • Multi-Tenant EF Core & Domain     │
└─────────────────────────────────┘   │ • SignalR Tracking Hub (/hubs/track)│
                                      │ • Rate Limiter & Security Headers   │
                                      └───────┬──────────────────────┬──────┘
                                              │                      │
                                              ▼                      ▼
                                      ┌───────────────┐      ┌──────────────┐
                                      │ POSTGRESQL 17 │      │   REDIS 7    │
                                      │ (Tenant Data) │      │  (Backplane) │
                                      └───────────────┘      └──────────────┘
```

---

## 2. Hardware Gateway & Network Configuration

### 2.1 Dedicated GPS Telemetry Ingestion
- **Device Type**: Physical hardwired bus tracker (e.g. CalAmp, Teltonika, Coban, Concox).
- **Transport**: Cellular 4G/LTE modem reporting directly to backend IP.
- **Protocol**: HTTP/JSON REST or TCP socket through `SchoolTrack.DeviceGateway`.
- **Target Endpoint**: `POST /api/v1/devices/gps/telemetry`
- **Payload Contract**:
  ```json
  {
    "deviceCode": "GPS-B14-01",
    "apiKey": "your-device-secret-key",
    "imei": "860123456789012",
    "latitude": 37.773500,
    "longitude": -122.420500,
    "speedKmh": 28.5,
    "heading": 115.0,
    "altitude": 45.0,
    "accuracyMeters": 0.8,
    "ignitionOn": true,
    "recordedAt": "2026-11-14T07:42:00Z"
  }
  ```
- **Offline Cellular Reconnect Flush**: `POST /api/v1/devices/gps/batch`

### 2.2 Bus Door Ethernet RFID Reader Ingestion
- **Device Type**: Ethernet RFID door reader (e.g. Impinj, Chainway, Wiegand-to-Ethernet Bridge).
- **Transport**: On-board industrial Ethernet switch connected to bus cellular router.
- **Independence**: Physical scans transmit directly to the backend and **never depend on the driver's phone**.
- **Target Endpoint**: `POST /api/v1/devices/rfid/tap`
- **Payload Contract**:
  ```json
  {
    "deviceCode": "RFID-B14-01",
    "apiKey": "your-reader-secret-key",
    "cardUid": "4B-99-F2-10",
    "latitude": 37.773500,
    "longitude": -122.420500,
    "scannedAt": "2026-11-14T07:42:15Z"
  }
  ```
- **Deduplication**: Built-in 15-second sliding cooldown window prevents accidental double-tap duplication.
- **Rogue Card Interception**: Unregistered cards are logged to `UnknownCardTap` security audit logs without disrupting reader uptime.

---

## 3. Production Deployment with Docker Compose

### 3.1 Prerequisites
- Docker Engine 24.0+ & Docker Compose v2.20+
- Minimum Server Specs: 2 vCPU, 4 GB RAM, 40 GB SSD.

### 3.2 Configuration File (`.env`)
Create `.env` alongside `docker-compose.yml`:
```bash
cp .env.example .env
```
Ensure strong cryptographic passwords are generated for production:
```ini
ASPNETCORE_ENVIRONMENT=Production
API_HTTP_PORT=5080
ADMIN_HTTP_PORT=5081

POSTGRES_DB=schooltrack
POSTGRES_USER=schooltrack_app
POSTGRES_PASSWORD=GENERATE_SUPER_SECURE_PASSWORD_2026!

REDIS_PORT=6379
REDIS_PASSWORD=GENERATE_SUPER_SECURE_REDIS_PASSWORD_2026!

JWT_ISSUER=SchoolTrack.Api
JWT_AUDIENCE=SchoolTrack.Clients
JWT_SECRET_KEY=GENERATE_AT_LEAST_32_CHAR_RANDOM_SECRET_KEY!
```

### 3.3 Running Containers
```bash
# Build and start services in background
docker compose up -d --build

# Inspect running containers
docker compose ps

# View real-time aggregated logs
docker compose logs -f api
```

---

## 4. Database Provisioning & Migrations

SchoolTrack enforces migration-based database schema management. Automatic schema alteration (`EnsureCreated`) is disabled in production.

### Method A: Automated Idempotent SQL Migration Script (Recommended)
An idempotent SQL script containing all 6 migrations with EF Core history tracking is generated at:
`docs/migrations/SchoolTrack_Production_All_Migrations.sql`

Apply via PostgreSQL CLI:
```bash
psql -h <POSTGRES_HOST> -p 5432 -U schooltrack_app -d schooltrack -f docs/migrations/SchoolTrack_Production_All_Migrations.sql
```

### Method B: .NET EF Tool Execution
```bash
dotnet ef database update --project src/SchoolTrack.Infrastructure --startup-project src/SchoolTrack.Api --configuration Release
```

---

## 5. Reverse Proxy & SSL/TLS Configuration

### 5.1 Nginx Configuration with WebSocket / SignalR Support
```nginx
upstream schooltrack_api {
    server 127.0.0.1:5080;
    keepalive 32;
}

upstream schooltrack_admin {
    server 127.0.0.1:5081;
}

# API & SignalR Tracking Hub
server {
    listen 443 ssl http2;
    server_name api.schooltrack.internal;

    ssl_certificate /etc/letsencrypt/live/api.schooltrack.internal/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/api.schooltrack.internal/privkey.pem;

    client_max_body_size 16M;

    location / {
        proxy_pass http://schooltrack_api;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection $http_connection;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        proxy_read_timeout 3600s;
        proxy_send_timeout 3600s;
    }
}

# Admin Web Application
server {
    listen 443 ssl http2;
    server_name admin.schooltrack.internal;

    ssl_certificate /etc/letsencrypt/live/admin.schooltrack.internal/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/admin.schooltrack.internal/privkey.pem;

    location / {
        proxy_pass http://schooltrack_admin;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection $http_connection;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

---

## 6. Security Hardening & Rate Limiting

1. **Security Headers Middleware**:
   - `X-Content-Type-Options: nosniff` (prevents MIME type confusion attacks)
   - `X-Frame-Options: SAMEORIGIN` (prevents clickjacking)
   - `X-XSS-Protection: 1; mode=block`
   - `Referrer-Policy: strict-origin-when-cross-origin`
   - `Strict-Transport-Security: max-age=31536000; includeSubDomains; preload`
   - Strip `Server` and `X-Powered-By` headers to avoid reconnaissance.

2. **Native ASP.NET Core Rate Limiting**:
   - **Auth Policy**: 15 requests / minute per IP on login and refresh endpoints.
   - **Hardware Policy**: 150 requests / minute per `X-Device-Code` or IP on telemetry endpoints.
   - **General API Policy**: Token bucket (80 burst, 60/min replenishment).

3. **Multi-Tenant Isolation**:
   - EF Core Global Query Filters automatically scope every query to `TenantEntity.SchoolId`.
   - Cross-school data leaks are cryptographically prevented at the ORM layer.

---

## 7. Health Checks & Observability

- **Liveness Probe**: `GET /health/live` (returns HTTP 200 `Healthy` when Kestrel process is alive).
- **Readiness Probe**: `GET /health/ready` (validates active PostgreSQL and Redis socket connectivity).
- **Comprehensive Probe**: `GET /health` (full diagnostics payload).

---

## 8. Backup & Disaster Recovery

### Automated PostgreSQL Backup Cron
```bash
#!/bin/bash
BACKUP_DIR="/var/backups/schooltrack"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
mkdir -p "$BACKUP_DIR"

docker exec schooltrack-postgres pg_dump -U schooltrack_app -d schooltrack -F c -b -v -f "/var/lib/postgresql/data/backup_${TIMESTAMP}.dump"
mv "/var/lib/docker/volumes/schooltrack_pgdata/_data/backup_${TIMESTAMP}.dump" "$BACKUP_DIR/"

# Retention: keep 14 daily backups
find "$BACKUP_DIR" -type f -name "*.dump" -mtime +14 -exec rm {} \;
```

