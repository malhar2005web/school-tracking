using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SchoolTrack.ParentApp.Models;
using SchoolTrack.Shared.DTOs.Auth;
using SchoolTrack.Shared.DTOs.Devices;
using SchoolTrack.Shared.DTOs.Trips;
using SchoolTrack.Shared.Models;

namespace SchoolTrack.ParentApp.Services;

public class ParentApiClient : IParentApiClient
{
    private readonly HttpClient _http;
    private readonly ParentAuthState _authState;
    private readonly ILogger<ParentApiClient> _logger;

    public ParentApiClient(HttpClient http, ParentAuthState authState, ILogger<ParentApiClient> logger)
    {
        _http = http;
        _authState = authState;
        _logger = logger;
    }

    private void ApplyHeaders()
    {
        var session = _authState.CurrentSession;
        if (session != null && !string.IsNullOrWhiteSpace(session.AccessToken))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
            _http.DefaultRequestHeaders.Remove("X-School-Id");
            _http.DefaultRequestHeaders.Add("X-School-Id", session.SchoolId.ToString());
        }
    }

    public async Task<ParentSession?> LoginWithPhoneAsync(string phoneNumber, string otpCode)
    {
        // Simulate remote OTP verification call
        await Task.Delay(100);

        var schoolId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var parentId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var leo = new ChildProfileModel(
            StudentId: Guid.Parse("55555555-5555-5555-5555-555555555555"),
            FullName: "Leo Martinez",
            Grade: "Grade 3",
            ClassSection: "Class 3B",
            AdmissionNumber: "ST-88219",
            SchoolName: "Oakridge Elementary",
            AvatarUrl: "https://lh3.googleusercontent.com/aida-public/AB6AXuBTqib1UkYXS7GXd37RC9bBFuiU6rnCQkSnFMdCE3wke9rA2nt28TuYFOp_FPMofqrch_jvnORoH13Qw0wPmf8uY3yUyRAuJhBA1qNRPlhE0-830V7bNfVUQMtOwfTdPJMSr3aHy_Z8POPXnvnmwH6tN0saGzXSOPUq_txdUAlrG4ooZN_scLox0cupFkd2lRpX9MZjezer-miEvaxa1jbxJ_HheAis9Yv2kzaH3zI",
            RfidCardUid: "4B-99-F2-10",
            IsRfidCardActive: true,
            BusId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            BusNumber: "Bus #14",
            RouteName: "Morning Route A • Oakridge Bound",
            PrimaryDriverName: "Robert Jenkins",
            DriverPhoneNumber: "+1 (555) 329-8472",
            DriverAvatarUrl: "https://lh3.googleusercontent.com/aida-public/AB6AXuAFrPPwe8Q228TD2qqumI7mbdRj2huhzXJ4IIeNLkiVyNwREAD3fXB8WR9UWI-f8oqn-DfnM14NsLr_fA8gCMOJ7DyskjIurGpVIpCCrZMohsoTpDMxaSHZuKEm3D4xBsoHZz1e8tkLGYnm8lCsOnMbHDKIngoN98ofTTAb1SYpTPusgVlhuXGmx9FQ0fUtpb6TsaDZGIjALJusVxxi2cofTOGO4YO2J4TES6BjmVM",
            DriverRating: 4.9,
            MorningPickupStopName: "Oakridge & 5th Ave",
            MorningPickupTime: "07:42 AM",
            AfternoonDropOffStopName: "Home Front Porch",
            AfternoonDropOffTime: "03:45 PM",
            IsOnBus: true
        );

        var maya = new ChildProfileModel(
            StudentId: Guid.Parse("66666666-6666-6666-6666-666666666666"),
            FullName: "Maya Martinez",
            Grade: "Grade 1",
            ClassSection: "Class 1A",
            AdmissionNumber: "ST-88220",
            SchoolName: "Oakridge Elementary",
            AvatarUrl: "https://lh3.googleusercontent.com/aida-public/AB6AXuD9dKIaQUU9h42sNd8oUEUv43uMcsxluN1p-E8T706D56YLD-sfigov8-XaEzIlPhe9oVk1pI1tsG8GhEgV_CayKJWU9J2Q_WMssM1w3YUIf-d3fs9ExbONRN3qgVUuQVU9oIf4244keIKmANrbecnnPIn7jucmKiNvCB-YFRb3oao93xnf5sE5EmLHBCwtSSGwMmrp0-X1p0_eIfoRphvfmMP4dhSCaXVQo1MYwB4",
            RfidCardUid: "7C-12-E5-88",
            IsRfidCardActive: true,
            BusId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            BusNumber: "Bus #14",
            RouteName: "Morning Route A • Oakridge Bound",
            PrimaryDriverName: "Robert Jenkins",
            DriverPhoneNumber: "+1 (555) 329-8472",
            DriverAvatarUrl: "https://lh3.googleusercontent.com/aida-public/AB6AXuAFrPPwe8Q228TD2qqumI7mbdRj2huhzXJ4IIeNLkiVyNwREAD3fXB8WR9UWI-f8oqn-DfnM14NsLr_fA8gCMOJ7DyskjIurGpVIpCCrZMohsoTpDMxaSHZuKEm3D4xBsoHZz1e8tkLGYnm8lCsOnMbHDKIngoN98ofTTAb1SYpTPusgVlhuXGmx9FQ0fUtpb6TsaDZGIjALJusVxxi2cofTOGO4YO2J4TES6BjmVM",
            DriverRating: 4.9,
            MorningPickupStopName: "Oakridge & 5th Ave",
            MorningPickupTime: "07:42 AM",
            AfternoonDropOffStopName: "Home Front Porch",
            AfternoonDropOffTime: "03:45 PM",
            IsOnBus: true
        );

        return new ParentSession(
            ParentUserId: parentId,
            SchoolId: schoolId,
            FullName: "Sarah Martinez",
            Email: "sarah.martinez@example.com",
            PhoneNumber: phoneNumber,
            AccessToken: "demo-jwt-token-parent-sarah",
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddDays(7),
            LinkedChildren: new List<ChildProfileModel> { leo, maya }
        );
    }

    public async Task<ParentSession?> LoginWithEmailAsync(string email, string password)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
                if (body?.Data != null)
                {
                    return await LoginWithPhoneAsync("+1 (555) 019-2834", "8421");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live API login call failed; falling back to demo parent session.");
        }

        return await LoginWithPhoneAsync("+1 (555) 019-2834", "8421");
    }

    public async Task<ChildActiveTripModel?> GetChildActiveTripAsync(Guid studentId, string shift = "Morning")
    {
        var effectiveShift = string.IsNullOrWhiteSpace(shift) ? _authState.CurrentShift : shift;

        try
        {
            ApplyHeaders();
            var res = await _http.GetFromJsonAsync<ApiResponse<LatestBusLocationDto>>($"/api/v1/buses/{Guid.Parse("22222222-2222-2222-2222-222222222222")}/location");
            if (res?.Data != null)
            {
                var loc = res.Data;
                return BuildActiveTrip(loc.Latitude, loc.Longitude, loc.SpeedKmh, loc.Heading, effectiveShift);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API GetChildActiveTrip failed; using simulated telemetry.");
        }

        return BuildActiveTrip(37.774929m, -122.419416m, 38.6m, 45m, effectiveShift);
    }

    private ChildActiveTripModel BuildActiveTrip(decimal lat, decimal lng, decimal speed, decimal heading, string shift)
    {
        var isMorning = shift.Equals("Morning", StringComparison.OrdinalIgnoreCase);
        var isClosed = _authState.IsTripClosed;
        var activeChild = _authState.ActiveChild;
        var childOnBus = activeChild?.IsOnBus ?? (!isClosed);

        List<TripWaypointModel> waypoints;
        string routeName;
        string destName;
        string approachingStop;
        string etaTime;
        int minsRemaining;
        int currentStopIndex;
        int totalStops;
        int completionPercent;
        string lastRfidInfo;
        string boardingPhase;
        string inOutStatus;
        string pickupStopEta;
        string destinationEta;

        if (isMorning)
        {
            routeName = "Morning Route A • Oakridge Bound (School)";
            destName = "Oakridge Elementary Campus";
            totalStops = 6;

            if (isClosed)
            {
                currentStopIndex = 6;
                completionPercent = 100;
                approachingStop = "Oakridge Elementary Campus (Arrived)";
                etaTime = "08:12 AM";
                minsRemaining = 0;
                boardingPhase = "SafelyDelivered";
                inOutStatus = "OUT (Safe Arrival at School)";
                pickupStopEta = "Completed at 07:42 AM";
                destinationEta = "Arrived safely at 08:12 AM";
                lastRfidInfo = "Safe Arrival Verified • Gate Tap at 08:12 AM";

                waypoints = new List<TripWaypointModel>
                {
                    new(1, "Oakridge & 5th Ave (Leo's Stop)", 37.7720m, -122.4220m, "07:42 AM", true, false, true),
                    new(2, "Oakridge Blvd & Pine St", 37.7735m, -122.4205m, "07:48 AM", true, false, false),
                    new(3, "Maple Crest Corner", 37.7749m, -122.4194m, "07:55 AM", true, false, false),
                    new(4, "Sunset Meadow Way", 37.7765m, -122.4178m, "08:02 AM", true, false, false),
                    new(5, "West River Gate", 37.7780m, -122.4160m, "08:05 AM", true, false, false),
                    new(6, "Oakridge Elementary Campus", 37.7795m, -122.4140m, "08:12 AM", true, true, false)
                };
            }
            else if (!childOnBus)
            {
                // Bus hasn't reached Leo's stop yet (Phase 1: Bus jar pickup point la nahi pohocli tar approx reaching time)
                currentStopIndex = 1;
                completionPercent = 15;
                approachingStop = "Oakridge & 5th Ave (Leo's Stop)";
                etaTime = "07:42 AM";
                minsRemaining = 7;
                boardingPhase = "BeforePickup";
                inOutStatus = "WAITING (At Pickup Stop)";
                pickupStopEta = "7 mins (07:42 AM)";
                destinationEta = "38 mins (08:15 AM)";
                lastRfidInfo = "Waiting for bus arrival at Oakridge & 5th Ave";

                waypoints = new List<TripWaypointModel>
                {
                    new(1, "Oakridge & 5th Ave (Leo's Stop)", 37.7720m, -122.4220m, "07:42 AM", false, true, true),
                    new(2, "Oakridge Blvd & Pine St", 37.7735m, -122.4205m, "07:48 AM", false, false, false),
                    new(3, "Maple Crest Corner", 37.7749m, -122.4194m, "07:55 AM", false, false, false),
                    new(4, "Sunset Meadow Way", 37.7765m, -122.4178m, "08:02 AM", false, false, false),
                    new(5, "West River Gate", 37.7780m, -122.4160m, "08:05 AM", false, false, false),
                    new(6, "Oakridge Elementary Campus", 37.7795m, -122.4140m, "08:15 AM", false, false, false)
                };
            }
            else
            {
                // Child has been picked up, bus is en route (Phase 2: pickup point varun nighali tar approx reaching time)
                currentStopIndex = 4;
                completionPercent = 65;
                approachingStop = "Sunset Meadow Way";
                etaTime = "08:05 AM";
                minsRemaining = 8;
                boardingPhase = "OnboardTransit";
                inOutStatus = "IN (On-Bus • Safe Transit)";
                pickupStopEta = "Picked up at 07:42 AM (Verified)";
                destinationEta = "18 mins (08:15 AM)";
                lastRfidInfo = "Tapped in at 07:42 AM (Oakridge & 5th Ave)";

                waypoints = new List<TripWaypointModel>
                {
                    new(1, "Oakridge & 5th Ave (Leo's Stop)", 37.7720m, -122.4220m, "07:42 AM", true, false, true),
                    new(2, "Oakridge Blvd & Pine St", 37.7735m, -122.4205m, "07:48 AM", true, false, false),
                    new(3, "Maple Crest Corner", 37.7749m, -122.4194m, "07:55 AM", true, false, false),
                    new(4, "Sunset Meadow Way", 37.7765m, -122.4178m, "08:02 AM", false, true, false),
                    new(5, "West River Gate", 37.7780m, -122.4160m, "08:05 AM", false, false, false),
                    new(6, "Oakridge Elementary Campus", 37.7795m, -122.4140m, "08:15 AM", false, false, false)
                };
            }
        }
        else
        {
            // Evening Shift (Returning from School - Yetana)
            routeName = "Evening Route B • Neighborhood Bound (Home)";
            destName = "Home Front Porch";
            totalStops = 6;

            if (isClosed)
            {
                currentStopIndex = 6;
                completionPercent = 100;
                approachingStop = "Home Front Porch (Arrived)";
                etaTime = "03:35 PM";
                minsRemaining = 0;
                boardingPhase = "SafelyDelivered";
                inOutStatus = "OUT (Deboarded at Home)";
                pickupStopEta = "Boarded at 03:02 PM";
                destinationEta = "Delivered safely at 03:35 PM";
                lastRfidInfo = "Safe Home Delivery • Parent Handover Confirmed";

                waypoints = new List<TripWaypointModel>
                {
                    new(1, "Oakridge Campus Gate (Departure)", 37.7795m, -122.4140m, "03:00 PM", true, false, false),
                    new(2, "West River Gate", 37.7780m, -122.4160m, "03:08 PM", true, false, false),
                    new(3, "Sunset Meadow Way", 37.7765m, -122.4178m, "03:15 PM", true, false, false),
                    new(4, "Maple Crest Corner", 37.7749m, -122.4194m, "03:22 PM", true, false, false),
                    new(5, "Oakridge Blvd & Pine St", 37.7735m, -122.4205m, "03:28 PM", true, false, false),
                    new(6, "Home Front Porch (Leo's Drop)", 37.7720m, -122.4220m, "03:35 PM", true, true, true)
                };
            }
            else if (!childOnBus)
            {
                // Child waiting at school for boarding
                currentStopIndex = 1;
                completionPercent = 10;
                approachingStop = "Oakridge Campus Gate (Boarding)";
                etaTime = "03:00 PM";
                minsRemaining = 5;
                boardingPhase = "BeforePickup";
                inOutStatus = "WAITING (At School Gate)";
                pickupStopEta = "Boarding starts in 5 mins (03:00 PM)";
                destinationEta = "40 mins (03:35 PM)";
                lastRfidInfo = "Waiting for afternoon boarding at Campus Gate";

                waypoints = new List<TripWaypointModel>
                {
                    new(1, "Oakridge Campus Gate (Departure)", 37.7795m, -122.4140m, "03:00 PM", false, true, false),
                    new(2, "West River Gate", 37.7780m, -122.4160m, "03:08 PM", false, false, false),
                    new(3, "Sunset Meadow Way", 37.7765m, -122.4178m, "03:15 PM", false, false, false),
                    new(4, "Maple Crest Corner", 37.7749m, -122.4194m, "03:22 PM", false, false, false),
                    new(5, "Oakridge Blvd & Pine St", 37.7735m, -122.4205m, "03:28 PM", false, false, false),
                    new(6, "Home Front Porch (Leo's Drop)", 37.7720m, -122.4220m, "03:35 PM", false, false, true)
                };
            }
            else
            {
                // Child boarded evening bus, heading home (Dual-phase ETA: reaching home)
                currentStopIndex = 4;
                completionPercent = 60;
                approachingStop = "Maple Crest Corner";
                etaTime = "03:22 PM";
                minsRemaining = 12;
                boardingPhase = "OnboardTransit";
                inOutStatus = "IN (On-Bus • Homeward Bound)";
                pickupStopEta = "Departed School at 03:02 PM";
                destinationEta = "13 mins (03:35 PM)";
                lastRfidInfo = "Afternoon Boarding Scanned at 03:02 PM";

                waypoints = new List<TripWaypointModel>
                {
                    new(1, "Oakridge Campus Gate (Departure)", 37.7795m, -122.4140m, "03:00 PM", true, false, false),
                    new(2, "West River Gate", 37.7780m, -122.4160m, "03:08 PM", true, false, false),
                    new(3, "Sunset Meadow Way", 37.7765m, -122.4178m, "03:15 PM", true, false, false),
                    new(4, "Maple Crest Corner", 37.7749m, -122.4194m, "03:22 PM", false, true, false),
                    new(5, "Oakridge Blvd & Pine St", 37.7735m, -122.4205m, "03:28 PM", false, false, false),
                    new(6, "Home Front Porch (Leo's Drop)", 37.7720m, -122.4220m, "03:35 PM", false, false, true)
                };
            }
        }

        var safeSummary = isClosed
            ? (isMorning
                ? $"{activeChild?.FullName ?? "Leo"} safely arrived at Oakridge Elementary Campus at 08:12 AM. Verified by RFID Gate Tap. Morning trip closed."
                : $"{activeChild?.FullName ?? "Leo"} safely reached Home Front Porch at 03:35 PM. Deboarded bus #14. Evening trip closed.")
            : null;

        return new ChildActiveTripModel(
            TripId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            SchoolId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            BusId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            BusNumber: "Bus #14",
            RouteName: routeName,
            DriverName: "Robert Jenkins",
            DriverPhone: "+1 (555) 329-8472",
            DriverAvatarUrl: "https://lh3.googleusercontent.com/aida-public/AB6AXuAFrPPwe8Q228TD2qqumI7mbdRj2huhzXJ4IIeNLkiVyNwREAD3fXB8WR9UWI-f8oqn-DfnM14NsLr_fA8gCMOJ7DyskjIurGpVIpCCrZMohsoTpDMxaSHZuKEm3D4xBsoHZz1e8tkLGYnm8lCsOnMbHDKIngoN98ofTTAb1SYpTPusgVlhuXGmx9FQ0fUtpb6TsaDZGIjALJusVxxi2cofTOGO4YO2J4TES6BjmVM",
            DriverRating: 4.9,
            CurrentLatitude: lat,
            CurrentLongitude: lng,
            SpeedKmh: isClosed ? 0 : speed,
            HeadingDegrees: heading,
            CurrentStreetLocation: isClosed ? destName : (isMorning ? "Maple Crest Corner near Elm St" : "West River Pkwy near Maple"),
            ApproachingStopName: approachingStop,
            EstimatedArrivalTime: etaTime,
            MinutesRemaining: minsRemaining,
            CurrentStopIndex: currentStopIndex,
            TotalStops: totalStops,
            CompletionPercentage: completionPercent,
            IsLive: !isClosed,
            WeatherStatus: "Clear 68°F",
            LastRfidBoardingInfo: lastRfidInfo,
            LastUpdated: DateTimeOffset.UtcNow,
            Waypoints: waypoints,
            Shift: shift,
            ChildBoardingPhase: boardingPhase,
            ChildInOutStatus: inOutStatus,
            PickupStopEtaText: pickupStopEta,
            DestinationEtaText: destinationEta,
            DestinationName: destName,
            IsTripClosed: isClosed,
            ClosedAt: _authState.ClosedAt,
            SafeArrivalSummary: safeSummary
        );
    }

    public async Task<List<RfidTransitLogModel>> GetChildRfidLogsAsync(Guid studentId, DateTime date)
    {
        await Task.Delay(50);

        var today = DateTimeOffset.UtcNow.Date;
        return new List<RfidTransitLogModel>
        {
            new(
                Guid.NewGuid(),
                new DateTimeOffset(today.Year, today.Month, today.Day, 7, 42, 0, TimeSpan.Zero),
                "07:42 AM",
                "Boarded Bus #14",
                "Oakridge & 5th Ave • Stop 1",
                "Boarding",
                "Bus #14",
                "Robert Jenkins",
                true,
                "https://lh3.googleusercontent.com/aida-public/AB6AXuBMWwFRkXo0XUcxllkBsKX_Qzc2hG1T0WdCWidGDJryI7xwRivgTym3aCxq0O6KjerTLbTTaSbBJ0POcUe9MFxFKTbXNr3LeYyQFCHVzL-dOVfCXnHpBsrOujSJk2h7xTXcv378Td40PcJdvLenUQ4cGud6JN6zLx-pZnTlzM6aCdUJMCrfhKc3McmgJQAb08dPnR52RNt_tg31BawhU2xb2zQT6MWTIDZoXh0ViGU",
                "Bus RFID Reader Ingested • Status: Verified OK"
            ),
            new(
                Guid.NewGuid(),
                new DateTimeOffset(today.Year, today.Month, today.Day, 7, 52, 0, TimeSpan.Zero),
                "07:52 AM",
                "Transit Gateway RFID Receiver",
                "Oakridge Blvd Transit Checkpoint",
                "EnRouteCheckpoint",
                "Bus #14",
                "Robert Jenkins",
                true,
                null,
                "Seatbelt & RFID telemetry active in-transit"
            ),
            new(
                Guid.NewGuid(),
                new DateTimeOffset(today.Year, today.Month, today.Day, 8, 5, 0, TimeSpan.Zero),
                "08:05 AM",
                "School Arrival & Gate Tap",
                "Oakridge West Turnstile #2",
                "SchoolGate",
                "Bus #14",
                "School Gate Staff",
                true,
                null,
                "Morning arrival logged • Safe on campus"
            )
        };
    }

    public async Task<bool> ReportAbsenceAsync(AbsenceReportRequest request)
    {
        await Task.Delay(100);
        _logger.LogInformation("Absence reported for Student {StudentId} on {Date:yyyy-MM-dd}: {Reason} - {Details}",
            request.StudentId, request.Date, request.Reason, request.Details);
        return true;
    }

    public async Task<ParentSafetySettings> GetSafetySettingsAsync(Guid studentId)
    {
        await Task.Delay(30);
        return new ParentSafetySettings();
    }

    public async Task<bool> SaveSafetySettingsAsync(Guid studentId, ParentSafetySettings settings)
    {
        await Task.Delay(50);
        _logger.LogInformation("Saved safety settings for student {StudentId}", studentId);
        return true;
    }

    public async Task<List<ParentNotificationModel>> GetNotificationsAsync()
    {
        await Task.Delay(30);
        return new List<ParentNotificationModel>
        {
            new(Guid.NewGuid(), "Leo Boarded Bus #14", "Leo scanned his RFID card at Oakridge & 5th Ave at 07:42 AM. Bus #14 is on schedule.", DateTimeOffset.UtcNow.AddMinutes(-20), "Boarding", false, "Leo Martinez"),
            new(Guid.NewGuid(), "Traffic Cleared", "Morning traffic cleared on Pine St corridor. ETA updated to 08:05 AM.", DateTimeOffset.UtcNow.AddMinutes(-12), "RouteAlert", false, "Leo Martinez"),
            new(Guid.NewGuid(), "Safe Arrival Streak", "Leo has reached a 5-day on-time transit streak! 🦉", DateTimeOffset.UtcNow.AddHours(-24), "General", true, "Leo Martinez"),
            new(Guid.NewGuid(), "Spring Schedule Notice", "Daylight savings transit timings take effect next Monday.", DateTimeOffset.UtcNow.AddDays(-2), "General", true, "All Students")
        };
    }

    public async Task<bool> MarkNotificationReadAsync(Guid notificationId)
    {
        await Task.Delay(20);
        return true;
    }

    public async Task<bool> TriggerEmergencyAlertAsync(Guid studentId, string reason)
    {
        await Task.Delay(100);
        _logger.LogWarning("EMERGENCY SOS ALERT triggered by parent for Student {StudentId}: {Reason}", studentId, reason);
        return true;
    }

    public async Task<List<AuthorizedGuardianModel>> GetAuthorizedGuardiansAsync(Guid studentId)
    {
        await Task.Delay(30);
        return new List<AuthorizedGuardianModel>
        {
            new(
                Guid.NewGuid(),
                "Sarah Martinez",
                "Mother (Primary)",
                "+1 (555) 019-2834",
                "https://lh3.googleusercontent.com/aida-public/AB6AXuDHDGGSb8ANzaCZHCqofS-D-q4PgE7LT1WDcNKEqXolmjZqzx7dVVsLOLj85ClQIyilXqCh1xf1tJkV8gKAjJl1_u4HtkDaiDkJLDGFcZWLsXzPvZwzKer8q8QksP-Qtn77MSo-jbMHx3Rv4Hei7m60y6xO321AHTZVm3-QFuxk0U03SJKd2ZlyuyTxQXYbOWTRwgEwJQBdCNMqwEJ0qxUON7pENAhrXVPYmVOgXRA",
                true,
                true
            ),
            new(
                Guid.NewGuid(),
                "David Martinez",
                "Father",
                "+1 (555) 019-2835",
                "https://lh3.googleusercontent.com/aida-public/AB6AXuAc8EXRAgYThWGqeZWKiHwwSZsJZuWwfV56NAihw8hVfP_WmudWpC73Nj-E49HFiBFuaufBTuw3SXfaTPSnGdqCsFx7oPOnEUoXWOGI6Cn7lzuCcz1Crr2ptaIEDQbTFLBMLTv-adVc7-tzVHszItCEf-qJjwr__afL4eBnaUgh9l7FbtFP9cMJu2OTcKRR4daweXL8hOUCdhDavt1YCk4DPKzXiG8Ubvg8CHmcgFs",
                false,
                true
            ),
            new(
                Guid.NewGuid(),
                "Elena Martinez",
                "Grandmother",
                "+1 (555) 019-2839",
                "https://lh3.googleusercontent.com/aida-public/AB6AXuDpLlyM0Broza9BqnBbgTBAhdP3Au4u0jnjcOLNGAdsbzyys417O6aUyWehYNuaZRB1Mw9wTMP6v4ayUVn7a7HCb5CzC1tZ-FIX8_ogG6eud--sgGqXEm9Czvb6keRmAvRFhCGJX7n6Haf8n8QYIVKOHMVDVeLxhflWT1wURn1ztnBhIQlcS23F9GdrI1EL9ckoZScqDdjZK3zPdY_hFHH0KgsDnxmc0xc7si-9LnQ",
                false,
                true
            )
        };
    }

    public async Task<bool> CloseParentTripAsync(Guid studentId)
    {
        await Task.Delay(50);
        _authState.CompleteTrip();
        return true;
    }

    public async Task<bool> ToggleSimulationChildBoardingStateAsync(Guid studentId, string nextState)
    {
        await Task.Delay(30);
        var isOnBus = nextState.Equals("OnboardTransit", StringComparison.OrdinalIgnoreCase);
        var inOut = nextState switch
        {
            "BeforePickup" => "WAITING (At Pickup Stop)",
            "OnboardTransit" => "IN (On-Bus • Safe Transit)",
            "SafelyDelivered" => "OUT (Safely Deboarded)",
            _ => "IN (On-Bus)"
        };
        _authState.UpdateActiveChildStatus(inOut, isOnBus);
        return true;
    }
}
