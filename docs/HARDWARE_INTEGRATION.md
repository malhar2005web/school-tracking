# Hardware Integration Strategy (RFID & Dedicated GPS)

## 1. Current Hardware Status

- **Bus RFID Reader**: Ethernet-connected hardware mounted on the bus; **exact manufacturer, model, and protocol are currently unknown**.
- **Bus GPS Tracker**: Dedicated hardware GPS device mounted on the bus; **exact manufacturer, model, and protocol are currently unknown**.
- **Constraint**: Neither RFID scans nor primary bus GPS coordinates may depend on the driver's mobile phone.

Because physical specifications and sample raw packets are not yet available, SchoolTrack isolates all wire protocols behind `SchoolTrack.DeviceGateway` adapter interfaces.

---

## 2. Gateway Architecture

```text
SchoolTrack.DeviceGateway/
├── RFID/
│   ├── Interfaces/IRfidProtocolAdapter.cs
│   ├── Adapters/         # HttpRfidAdapter, TcpRfidAdapter, UdpRfidAdapter, VendorRfidAdapter (added when verified)
│   ├── Services/         # RfidIngestionService, RfidEventProcessor, RfidDeviceAuthenticationService
│   ├── DTOs/             # RawRfidPayload, NormalizedRfidEvent
│   └── Simulators/       # RfidDeviceSimulator (dev/test only)
├── GPS/
│   ├── Interfaces/IGpsProtocolAdapter.cs
│   ├── Adapters/         # HttpGpsAdapter, TcpGpsAdapter, UdpGpsAdapter, MqttGpsAdapter, VendorGpsAdapter
│   ├── Services/         # GpsIngestionService, GpsLocationProcessor, GpsDeviceAuthenticationService, GpsHeartbeatService
│   ├── DTOs/             # RawGpsPayload, NormalizedGpsEvent
│   └── Simulators/       # GpsDeviceSimulator (dev/test only)
└── Registry/             # Adapter resolution by ProtocolType
```

### Normalized Contracts
- **`NormalizedRfidEvent`**: `DeviceId`, `CardUid`, `RecordedAt` (`DateTimeOffset`), `SequenceNumber` (`long?`), `EventId` (`string?`), `SignalRssi` (`int?`).
- **`NormalizedGpsEvent`**: `DeviceId`, `Latitude`, `Longitude`, `SpeedKmh` (`double?`), `Heading` (`double?`), `AccuracyMeters` (`double?`), `RecordedAt` (`DateTimeOffset`), `SequenceNumber` (`long?`).

---

## 3. Security & Reliability Rules

1. **Zero Client-Supplied Trust for Tenant/Bus/Student**:
   - Devices authenticate with server-issued `DeviceCredentials` (`KeyIdentifier` + rotated secret/HMAC or certificate).
   - `SchoolId` and `BusId` are resolved exclusively from the authenticated `GpsDevice` / `RfidDevice` record.
   - `StudentId` is resolved exclusively from `RfidCards(SchoolId, CardUid)`.
2. **Idempotency & Deduplication**:
   - RFID: Deduplicate via `(RfidDeviceId, EventId)`, `(RfidDeviceId, SequenceNumber)`, and configurable duplicate-tap cooldown window (`SchoolSettings.DuplicateScanWindowSeconds`).
   - GPS: Reject invalid coordinates (`lat` outside `[-90,90]`, `lng` outside `[-180,180]`, `(0,0)` null-island filter unless flagged) and future-skewed timestamps; accept out-of-order historical packets into `LocationLogs` without overwriting newer `LatestBusLocations`.
3. **Offline Buffering**:
   - Batch endpoints/pipelines accept chronological arrays of buffered events inside DB transactions.
4. **Device Health**:
   - Track `LastSeenAt`, `DeviceHeartbeats`, and background offline detection (`DeviceOfflineThresholdSeconds`).

---

## 4. Phase 11 Onboarding Checklist (When Hardware Docs Arrive)

For each physical RFID reader and GPS tracker model:
1. Record manufacturer, model, firmware version, and transport protocol (TCP/UDP/HTTP/MQTT).
2. Capture and archive sample raw packets (including login/heartbeat/data/offline-flush packets).
3. Implement `IRfidProtocolAdapter` / `IGpsProtocolAdapter` for the verified format.
4. Verify CRC/checksum, timezone/UTC conversion, and ACK frame requirements.
5. Add unit tests using captured raw packet fixtures, then test against physical bench hardware.

