using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SchoolTrack.DriverApp.Models;
using SchoolTrack.Shared.DTOs.Auth;
using SchoolTrack.Shared.DTOs.Devices;
using SchoolTrack.Shared.DTOs.Trips;
using SchoolTrack.Shared.Models;

namespace SchoolTrack.DriverApp.Services;

public class DriverApiClient : IDriverApiClient
{
    private readonly HttpClient _http;
    private readonly DriverAuthState _authState;
    private readonly ILogger<DriverApiClient> _logger;

    public DriverApiClient(HttpClient http, DriverAuthState authState, ILogger<DriverApiClient> logger)
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

    public async Task<DriverSession?> LoginAsync(string licenseNumber, string pin)
    {
        await Task.Delay(100);

        var schoolId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var driverId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var busId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var routeId = Guid.Parse("88888888-8888-8888-8888-888888888888");

        return new DriverSession(
            DriverId: driverId,
            SchoolId: schoolId,
            FullName: "Robert Jenkins",
            LicenseNumber: licenseNumber,
            PhoneNumber: "+1 (555) 329-8472",
            AvatarUrl: "https://lh3.googleusercontent.com/aida-public/AB6AXuAFrPPwe8Q228TD2qqumI7mbdRj2huhzXJ4IIeNLkiVyNwREAD3fXB8WR9UWI-f8oqn-DfnM14NsLr_fA8gCMOJ7DyskjIurGpVIpCCrZMohsoTpDMxaSHZuKEm3D4xBsoHZz1e8tkLGYnm8lCsOnMbHDKIngoN98ofTTAb1SYpTPusgVlhuXGmx9FQ0fUtpb6TsaDZGIjALJusVxxi2cofTOGO4YO2J4TES6BjmVM",
            BusId: busId,
            BusNumber: "Bus #14",
            RouteId: routeId,
            RouteName: "Morning Route 14-AM • Oakridge East",
            AccessToken: "demo-driver-jwt-token-robert",
            ShiftStatus: "InTransit"
        );
    }

    public async Task<DriverActiveRouteModel?> GetActiveRouteAsync(Guid busId)
    {
        try
        {
            ApplyHeaders();
            var res = await _http.GetFromJsonAsync<ApiResponse<LatestBusLocationDto>>($"/api/v1/buses/{busId}/location");
            if (res?.Data != null)
            {
                var loc = res.Data;
                return BuildDriverRoute(loc.SpeedKmh);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API GetActiveRoute failed; using simulated driver route.");
        }

        return BuildDriverRoute(22m);
    }

    private DriverActiveRouteModel BuildDriverRoute(decimal speed)
    {
        var stops = new List<DriverActiveStopModel>
        {
            new(Guid.NewGuid(), "Willow Creek Estates", 1, "07:15 AM", "07:14 AM", 4, 4, false, true, 37.7680m, -122.4260m),
            new(Guid.NewGuid(), "Pine Valley Crossing", 2, "07:25 AM", "07:24 AM", 6, 6, false, true, 37.7700m, -122.4240m),
            new(Guid.NewGuid(), "Sunset Ridge Blvd", 3, "07:35 AM", "07:35 AM", 7, 7, false, true, 37.7715m, -122.4225m),
            new(Guid.Parse("44444444-4444-4444-4444-444444444441"), "Oakridge & 5th Ave", 4, "07:42 AM", "07:42 AM", 3, 2, true, false, 37.7735m, -122.4205m),
            new(Guid.Parse("44444444-4444-4444-4444-444444444442"), "Maple Crest Corner", 5, "07:50 AM", "07:51 AM", 3, 0, false, false, 37.7749m, -122.4194m),
            new(Guid.NewGuid(), "Cedar Springs Way", 6, "07:58 AM", "07:59 AM", 2, 0, false, false, 37.7765m, -122.4178m),
            new(Guid.NewGuid(), "West River Gate", 7, "08:05 AM", "08:05 AM", 2, 0, false, false, 37.7780m, -122.4160m),
            new(Guid.NewGuid(), "Oakridge Elementary Campus", 8, "08:12 AM", "08:12 AM", 0, 0, false, false, 37.7795m, -122.4140m)
        };

        return new DriverActiveRouteModel(
            TripId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            RouteId: Guid.Parse("88888888-8888-8888-8888-888888888888"),
            RouteName: "Morning Route 14-AM • Oakridge East",
            BusId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            BusNumber: "Bus #14",
            SpeedKmh: speed,
            SpeedLimitKmh: 25,
            NextTurnInstruction: "Turn Left on Evergreen Blvd",
            DistanceToNextTurn: "300 Feet",
            NextStopName: "Stop 4: Oakridge & 5th Ave",
            GpsAccuracyMeters: 0.8m,
            RfidHardwareOnline: true,
            OnboardCount: 24,
            ToPickUpCount: 3,
            SchoolTargetCount: 27,
            CurrentStopIndex: 4,
            TotalStops: 8,
            Stops: stops
        );
    }

    public async Task<List<DriverManifestStudentModel>> GetStudentManifestAsync(Guid tripId)
    {
        await Task.Delay(50);

        var stop4Id = Guid.Parse("44444444-4444-4444-4444-444444444441");
        var stop5Id = Guid.Parse("44444444-4444-4444-4444-444444444442");

        return new List<DriverManifestStudentModel>
        {
            new()
            {
                StudentId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                FullName = "Leo Martinez",
                Grade = "Grade 3-B",
                ClassSection = "Class 3B",
                SeatNumber = "Seat 4A",
                RfidCardUid = "4B-99-F2-10",
                StopId = stop4Id,
                StopName = "Oakridge & 5th Ave",
                StopSequence = 4,
                IsBoarded = true,
                BoardedAt = DateTimeOffset.UtcNow.AddMinutes(-12),
                BoardingMethod = "RfidScan",
                IsAbsent = false,
                AvatarUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuAkyDMm2dzl3XDcpOaJbEVvnNS8NekmSKt7Uca-SvlUjA68XCvIjCXg5E3FyIf2FNezik2kmA_Z5Tgz5uxykk9KZITK_iA4orXjS3Pqmq1un4uFkR-c5JbKgZeQr_-hWQ14FlP-hLhODLJx9Uemf3uF3N5CY1iyLkaqqzLAy68T5-J9vVWIMoOikG27VyfClmqBQNfTRhioPRv3toeamXc-uW4nnW3DWRZFEegSgUE",
                ParentName = "Sarah Martinez",
                ParentPhone = "+1 (555) 019-2834"
            },
            new()
            {
                StudentId = Guid.NewGuid(),
                FullName = "Sophia Chen",
                Grade = "Grade 4-A",
                ClassSection = "Class 4A",
                SeatNumber = "Seat 4B",
                RfidCardUid = "1A-88-C3-44",
                StopId = stop4Id,
                StopName = "Oakridge & 5th Ave",
                StopSequence = 4,
                IsBoarded = true,
                BoardedAt = DateTimeOffset.UtcNow.AddMinutes(-11),
                BoardingMethod = "RfidScan",
                IsAbsent = false,
                AvatarUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuBeqX_RWMEPDfhoHH4zW5QGoHrr96sV4con3TuxcEQ6WaSGCWSsb-I6BQMPuLZYbxcJDxJJAte3hLCqJpsP4kN9YA5mXb_zHllNROgMPyU8mTN_hK_2vdVkPA7eQwRl5pE-3kqdB7Zl4AtQNGPhe-c9_JyqHrsc5Btyj14gG1-JG09Ex5c5SA4BtWR9z_i_1P1ulY1hoCW-qGRWtF599BVMWqe0GUSMZbR3BBY_bMs",
                ParentName = "David Chen",
                ParentPhone = "+1 (555) 019-5821"
            },
            new()
            {
                StudentId = Guid.NewGuid(),
                FullName = "Lucas Silva",
                Grade = "Grade 3-B",
                ClassSection = "Class 3B",
                SeatNumber = "Seat 5A",
                RfidCardUid = "9D-33-F1-02",
                StopId = stop4Id,
                StopName = "Oakridge & 5th Ave",
                StopSequence = 4,
                IsBoarded = false,
                BoardedAt = null,
                BoardingMethod = null,
                IsAbsent = false,
                AvatarUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuAznCRC7NdmL7vPhQkJx_yUGb5MXFMQtxwA67ohd9_fvHOdFwaeiMPBD7kFg-j-Tv2lRld9s51C3c1lZjCEZiTRmti1ioJcWKFtm_0tCd0jO38dVpA0GETFgdsVtN7uo3R2IRW3F2Kec1vmp28_Z_3lV1nEjHKCQTAHuSrlcpEGgzBxCABZ1XGebeyLv15zIMFOjehxESYYoAGBjQC_JdrGtk2iuK1nUZIA4aOYzmY",
                ParentName = "Maria Silva",
                ParentPhone = "+1 (555) 019-9943"
            },
            new()
            {
                StudentId = Guid.NewGuid(),
                FullName = "Oliver Brown",
                Grade = "Grade 2-C",
                ClassSection = "Class 2C",
                SeatNumber = "Seat 6B",
                RfidCardUid = "6B-44-E8-19",
                StopId = stop5Id,
                StopName = "Maple Crest Corner",
                StopSequence = 5,
                IsBoarded = false,
                BoardedAt = null,
                BoardingMethod = null,
                IsAbsent = false,
                AvatarUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuAwNDZ2mzANmkMfLL99_mXlReborLNeG6ElS2EEM_88An_0p-HaoHTX-HQRNBW4no8vVrwRsdHjrOgjwIlCV8t5TWZFZAUF6wld69TTawiiZzQFD7jqVAwfNU5_qW3jMHEF2TFnhC4SjSoaVRlt8tT-XfYmdPlrq75-BrfEbTDwGL4UtivOHWGPE3PLd9Iox3JXVzByeWiXBqzxXkXte0I8xZw18ZprZEpf9KY-E2s",
                ParentName = "James Brown",
                ParentPhone = "+1 (555) 019-4820"
            },
            new()
            {
                StudentId = Guid.NewGuid(),
                FullName = "Chloe Davis",
                Grade = "Grade 3-A",
                ClassSection = "Class 3A",
                SeatNumber = "Seat 7A",
                RfidCardUid = "3C-11-AA-72",
                StopId = stop5Id,
                StopName = "Maple Crest Corner",
                StopSequence = 5,
                IsBoarded = false,
                BoardedAt = null,
                BoardingMethod = null,
                IsAbsent = true,
                AbsenceReason = "Doctor Appt (Parent App)",
                AvatarUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuAdpAgSXO9EM5xskcS1kGYJH-Sczxn1Pou8PajONH4xrCUDVfT8dKLUpflQ0RYN5fT5BE594cicCUX_PLsYSyuiPhCR0SgHvUYmli6HZ4xR_H9bUNsYV_tOEqQ4Kk0sXjFLV-QqdoUufkLySkJVPxlOZ5u--RD8S3F2WMDf8raXurpS0Lc1x_TNX7mdCbwspDzJ5tmdYEcwWY_knHyp012cng2lL7zqMydV-GsJbug",
                ParentName = "Rebecca Davis",
                ParentPhone = "+1 (555) 019-7711"
            }
        };
    }

    public async Task<bool> ManualCheckInStudentAsync(Guid tripId, Guid studentId, string reason)
    {
        await Task.Delay(50);
        _logger.LogInformation("Driver manual check-in recorded for student {StudentId} on trip {TripId}: {Reason}",
            studentId, tripId, reason);
        return true;
    }

    public async Task<bool> MarkStudentAbsentAsync(Guid tripId, Guid studentId, string reason)
    {
        await Task.Delay(50);
        _logger.LogInformation("Driver marked student {StudentId} absent on trip {TripId}: {Reason}",
            studentId, tripId, reason);
        return true;
    }

    public async Task<bool> CompleteStopAsync(Guid tripId, Guid stopId)
    {
        await Task.Delay(50);
        _logger.LogInformation("Driver completed boarding for stop {StopId} on trip {TripId}", stopId, tripId);
        return true;
    }

    public async Task<bool> BroadcastDelayAsync(Guid tripId, BroadcastDelayRequest request)
    {
        await Task.Delay(100);
        _logger.LogWarning("Driver broadcasted delay of +{Minutes} mins for trip {TripId}: {Reason} - {Notes}",
            request.MinutesDelay, tripId, request.Reason, request.CustomNotes);
        return true;
    }

    public async Task<bool> SubmitPreTripChecklistAsync(Guid busId, DriverPreTripChecklist checklist)
    {
        await Task.Delay(50);
        _logger.LogInformation("Submitted pre-trip inspection checklist for Bus {BusId}", busId);
        return true;
    }

    public async Task<bool> TriggerEmergencySosAsync(DriverSosAlertRequest request)
    {
        await Task.Delay(100);
        _logger.LogCritical("🚨 DRIVER EMERGENCY SOS TRANSMITTED from Bus {BusNumber} at ({Lat}, {Lng}): {Reason}",
            request.BusNumber, request.Latitude, request.Longitude, request.Reason);
        return true;
    }
}

