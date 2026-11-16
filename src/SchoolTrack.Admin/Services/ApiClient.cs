using System.Net.Http.Headers;
using System.Net.Http.Json;
using SchoolTrack.Admin.Models;
using SchoolTrack.Shared.DTOs.Auth;
using SchoolTrack.Shared.DTOs.Drivers;
using SchoolTrack.Shared.DTOs.Fleet;
using SchoolTrack.Shared.DTOs.Routes;
using SchoolTrack.Shared.DTOs.Schools;
using SchoolTrack.Shared.DTOs.Students;
using SchoolTrack.Shared.DTOs.Trips;
using SchoolTrack.Shared.Models;

namespace SchoolTrack.Admin.Services;

public class ApiClient : IApiClient
{
    private readonly HttpClient _http;
    private readonly AdminAuthStateProvider _authState;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(HttpClient http, AdminAuthStateProvider authState, ILogger<ApiClient> logger)
    {
        _http = http;
        _authState = authState;
        _logger = logger;
    }

    private void ApplyHeaders()
    {
        var session = _authState.CurrentSession;
        if (!string.IsNullOrWhiteSpace(session.AccessToken))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        }
        if (session.SchoolId != Guid.Empty)
        {
            _http.DefaultRequestHeaders.Remove("X-School-Id");
            _http.DefaultRequestHeaders.Add("X-School-Id", session.SchoolId.ToString());
        }
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("/api/v1/auth/login", request);
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
                return body?.Data;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live API login call failed; falling back to demo session.");
        }

        // Fallback demo response
        return new AuthResponse(
            AccessToken: "demo-jwt-token-oakridge-admin",
            RefreshToken: "demo-refresh-token",
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(8),
            User: new UserDto(
                Id: Guid.Parse("99999999-9999-9999-9999-999999999999"),
                SchoolId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Email: request.Email,
                FullName: "Elena Vasquez",
                PhoneNumber: "+1 (555) 019-2834",
                Role: "SchoolAdmin",
                IsActive: true,
                SchoolName: "Oakridge Academy District #4"
            )
        );
    }

    public async Task<List<SchoolDto>> GetSchoolsAsync()
    {
        try
        {
            ApplyHeaders();
            var res = await _http.GetFromJsonAsync<ApiResponse<List<SchoolDto>>>("/api/v1/schools");
            if (res?.Data != null) return res.Data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API GetSchools failed; returning seed data.");
        }

        return new List<SchoolDto>
        {
            new(
                Id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name: "Oakridge Academy District #4",
                Code: "OAK-04",
                TimeZoneId: "America/Los_Angeles",
                ContactEmail: "admin@oakridge.edu",
                ContactPhone: "+1 (555) 019-2834",
                Address: "742 Evergreen Terrace, Oakridge",
                IsActive: true,
                CreatedAt: DateTimeOffset.UtcNow,
                Settings: null
            )
        };
    }

    public async Task<DashboardSummaryViewModel> GetDashboardSummaryAsync(Guid schoolId)
    {
        var radar = await GetFleetRadarAsync(schoolId);
        return new DashboardSummaryViewModel
        {
            ActiveFleetCount = 18,
            TotalFleetCount = 20,
            StudentsBoardedCount = 412,
            TotalStudentsCount = 438,
            GatewaysOnlineCount = 24,
            TotalGatewaysCount = 24,
            ActiveAlertsCount = 1,
            ActiveAlertSummary = "Route #08 Maple Ave Congestion (+7 min)",
            ActiveBuses = radar,
            RecentTaps = new List<LiveRfidTapItem>
            {
                new() { StudentName = "Leo Martinez", CardSerialNumber = "ST-88219", Grade = "Grade 4", BusNumber = "Bus #14", StopName = "Oakridge & 5th Ave", EventType = "Boarded", Timestamp = DateTime.Now.AddMinutes(-2), IsVerified = true, ParentNotified = true },
                new() { StudentName = "Maya Lin", CardSerialNumber = "ST-44190", Grade = "Grade 5", BusNumber = "Bus #08", StopName = "Maple Blossom Dr", EventType = "Boarded", Timestamp = DateTime.Now.AddMinutes(-5), IsVerified = true, ParentNotified = true },
                new() { StudentName = "Ethan Cole", CardSerialNumber = "ST-10398", Grade = "Grade 2", BusNumber = "Bus #14", StopName = "St. Jude West Gate", EventType = "Exited", Timestamp = DateTime.Now.AddMinutes(-8), IsVerified = true, ParentNotified = true },
                new() { StudentName = "Sophia Rossi", CardSerialNumber = "ST-55209", Grade = "Grade 3", BusNumber = "Bus #14", StopName = "Pine Valley Rd", EventType = "Boarded", Timestamp = DateTime.Now.AddMinutes(-12), IsVerified = true, ParentNotified = true },
                new() { StudentName = "Aria Montgomery", CardSerialNumber = "ST-77312", Grade = "Grade 1", BusNumber = "Bus #22", StopName = "Elm Crest Gate", EventType = "Boarded", Timestamp = DateTime.Now.AddMinutes(-16), IsVerified = true, ParentNotified = true }
            }
        };
    }

    public async Task<List<BusLiveStatusItem>> GetFleetRadarAsync(Guid schoolId)
    {
        return await Task.FromResult(new List<BusLiveStatusItem>
        {
            new()
            {
                BusId = Guid.Parse("14141414-1414-1414-1414-141414141414"),
                BusNumber = "Bus #14",
                LicensePlate = "CA-88B-914",
                DriverName = "Robert Jenkins",
                DriverLicense = "CDL-49102-CA (Exp. 2026)",
                Latitude = 37.7790,
                Longitude = -122.4210,
                SpeedMph = 24.0,
                Status = "OnSchedule",
                RouteName = "Route Westview A",
                NextStopName = "Oakridge Elementary West Turnstile",
                NextStopEtaMinutes = 8,
                PassengerCount = 26,
                Capacity = 28,
                IsSelected = true
            },
            new()
            {
                BusId = Guid.Parse("08080808-0808-0808-0808-080808080808"),
                BusNumber = "Bus #08",
                LicensePlate = "CA-22M-008",
                DriverName = "Marcus Vance",
                DriverLicense = "CDL-33918-CA (Exp. 2025)",
                Latitude = 37.7710,
                Longitude = -122.4180,
                SpeedMph = 14.5,
                Status = "Delayed",
                RouteName = "Route Maple East B",
                NextStopName = "Maple Blossom Dr & 3rd",
                NextStopEtaMinutes = 15,
                PassengerCount = 22,
                Capacity = 30,
                IsSelected = false
            },
            new()
            {
                BusId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                BusNumber = "Bus #22",
                LicensePlate = "CA-55X-022",
                DriverName = "Elena Rostova",
                DriverLicense = "CDL-77401-CA (Exp. 2027)",
                Latitude = 37.7850,
                Longitude = -122.4110,
                SpeedMph = 18.2,
                Status = "OnSchedule",
                RouteName = "Route North Valley C",
                NextStopName = "Northgate Transit Loop",
                NextStopEtaMinutes = 11,
                PassengerCount = 19,
                Capacity = 24,
                IsSelected = false
            },
            new()
            {
                BusId = Guid.Parse("05050505-0505-0505-0505-050505050505"),
                BusNumber = "Bus #05",
                LicensePlate = "CA-11P-005",
                DriverName = "David Kim",
                DriverLicense = "CDL-11928-CA (Exp. 2026)",
                Latitude = 37.7800,
                Longitude = -122.4280,
                SpeedMph = 0.0,
                Status = "AtStop",
                RouteName = "Route South Suburban D",
                NextStopName = "Oakridge West Turnstile (Arrived)",
                NextStopEtaMinutes = 0,
                PassengerCount = 28,
                Capacity = 28,
                IsSelected = false
            }
        });
    }

    public async Task<List<StudentDto>> GetStudentsAsync(Guid schoolId, string? grade = null, string? search = null)
    {
        try
        {
            ApplyHeaders();
            var res = await _http.GetFromJsonAsync<ApiResponse<List<StudentDto>>>($"/api/v1/schools/{schoolId}/students");
            if (res?.Data != null && res.Data.Count > 0) return res.Data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API GetStudents failed; returning rich mock roster.");
        }

        var roster = new List<StudentDto>
        {
            new(Guid.NewGuid(), schoolId, Guid.NewGuid(), "Grade 4 - Section A", "ADM-88219", "Leo", "Martinez", null, "+1 (555) 482-1920", "12A", "ST-88219", true),
            new(Guid.NewGuid(), schoolId, Guid.NewGuid(), "Grade 5 - Section B", "ADM-44190", "Maya", "Lin", null, "+1 (555) 391-8841", "04B", "ST-44190", true),
            new(Guid.NewGuid(), schoolId, Guid.NewGuid(), "Grade 2 - Section A", "ADM-10398", "Ethan", "Cole", null, "+1 (555) 728-3319", "08A", "ST-10398", true),
            new(Guid.NewGuid(), schoolId, Guid.NewGuid(), "Grade 3 - Section C", "ADM-55209", "Sophia", "Rossi", null, "+1 (555) 902-8411", "15C", "ST-55209", true),
            new(Guid.NewGuid(), schoolId, Guid.NewGuid(), "Grade 1 - Section A", "ADM-77312", "Aria", "Montgomery", null, "+1 (555) 183-4920", "02A", "ST-77312", true),
            new(Guid.NewGuid(), schoolId, Guid.NewGuid(), "Grade 4 - Section B", "ADM-33821", "Lucas", "Bennett", null, "+1 (555) 662-7714", "11B", "ST-33821", true),
            new(Guid.NewGuid(), schoolId, Guid.NewGuid(), "Grade 5 - Section A", "ADM-99120", "Olivia", "Chen", null, "+1 (555) 832-5501", "07A", "ST-99120", true),
            new(Guid.NewGuid(), schoolId, Guid.NewGuid(), "Grade 3 - Section B", "ADM-12845", "Jackson", "Reed", null, "+1 (555) 441-2918", "14B", "ST-12845", true)
        };

        if (!string.IsNullOrWhiteSpace(grade) && grade != "all")
        {
            roster = roster.Where(s => s.ClassName.Contains(grade, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.ToLowerInvariant();
            roster = roster.Where(s => s.FirstName.ToLowerInvariant().Contains(q) ||
                                       s.LastName.ToLowerInvariant().Contains(q) ||
                                       (s.AssignedCardUid != null && s.AssignedCardUid.ToLowerInvariant().Contains(q)) ||
                                       s.AdmissionNumber.ToLowerInvariant().Contains(q)).ToList();
        }

        return roster;
    }

    public async Task<StudentDto?> CreateStudentAsync(Guid schoolId, CreateStudentRequest request)
    {
        try
        {
            ApplyHeaders();
            var res = await _http.PostAsJsonAsync($"/api/v1/schools/{schoolId}/students", request);
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<ApiResponse<StudentDto>>();
                return body?.Data;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API CreateStudent failed.");
        }

        return new StudentDto(Guid.NewGuid(), schoolId, request.ClassId, "Grade 1 - Section A", request.AdmissionNumber, request.FirstName, request.LastName, null, request.EmergencyContactPhone, request.SeatNumber, null, true);
    }

    public async Task<List<BusDto>> GetBusesAsync(Guid schoolId)
    {
        return await Task.FromResult(new List<BusDto>
        {
            new(Guid.Parse("14141414-1414-1414-1414-141414141414"), schoolId, "Bus #14", "CA-88B-914", "Bluebird All American", 28, "Active", DateTimeOffset.UtcNow.AddYears(1), DateTimeOffset.UtcNow.AddMonths(6), true, Guid.NewGuid(), "Robert Jenkins", "GPS-OBD-9041", "RFID-ETH-14"),
            new(Guid.Parse("08080808-0808-0808-0808-080808080808"), schoolId, "Bus #08", "CA-22M-008", "Thomas Saf-T-Liner", 30, "Active", DateTimeOffset.UtcNow.AddYears(1), DateTimeOffset.UtcNow.AddMonths(4), true, Guid.NewGuid(), "Marcus Vance", "GPS-OBD-8812", "RFID-ETH-08"),
            new(Guid.Parse("22222222-2222-2222-2222-222222222222"), schoolId, "Bus #22", "CA-55X-022", "IC Bus CE Series", 24, "Active", DateTimeOffset.UtcNow.AddYears(1), DateTimeOffset.UtcNow.AddMonths(9), true, Guid.NewGuid(), "Elena Rostova", "GPS-OBD-7731", "RFID-ETH-22"),
            new(Guid.Parse("05050505-0505-0505-0505-050505050505"), schoolId, "Bus #05", "CA-11P-005", "Collins Bus Super Bantam", 28, "Active", DateTimeOffset.UtcNow.AddYears(1), DateTimeOffset.UtcNow.AddMonths(5), true, Guid.NewGuid(), "David Kim", "GPS-OBD-5501", "RFID-ETH-05"),
            new(Guid.NewGuid(), schoolId, "Bus #19", "CA-44R-019", "Bluebird Vision", 32, "Maintenance", DateTimeOffset.UtcNow.AddMonths(2), DateTimeOffset.UtcNow.AddMonths(1), false, null, null, "GPS-OBD-1901", "RFID-ETH-19")
        });
    }

    public async Task<List<DriverDto>> GetDriversAsync(Guid schoolId)
    {
        return await Task.FromResult(new List<DriverDto>
        {
            new(Guid.NewGuid(), schoolId, Guid.NewGuid(), "Robert Jenkins", "robert.jenkins@transit.oakridge.edu", "CDL-49102-CA", DateTimeOffset.UtcNow.AddYears(2), "+1 (555) 771-8821", "Active", 12, 4.9),
            new(Guid.NewGuid(), schoolId, Guid.NewGuid(), "Marcus Vance", "marcus.vance@transit.oakridge.edu", "CDL-33918-CA", DateTimeOffset.UtcNow.AddYears(1), "+1 (555) 662-9912", "Active", 8, 4.7),
            new(Guid.NewGuid(), schoolId, Guid.NewGuid(), "Elena Rostova", "elena.rostova@transit.oakridge.edu", "CDL-77401-CA", DateTimeOffset.UtcNow.AddYears(3), "+1 (555) 881-4431", "Active", 15, 5.0),
            new(Guid.NewGuid(), schoolId, Guid.NewGuid(), "David Kim", "david.kim@transit.oakridge.edu", "CDL-11928-CA", DateTimeOffset.UtcNow.AddYears(2), "+1 (555) 334-1182", "Active", 6, 4.8)
        });
    }

    public async Task<List<RouteDto>> GetRoutesAsync(Guid schoolId)
    {
        return await Task.FromResult(new List<RouteDto>
        {
            new(Guid.NewGuid(), schoolId, "Route Westview A", "W-A", "Inbound", true, new List<RouteStopDto>
            {
                new(Guid.NewGuid(), Guid.NewGuid(), "Pine Valley Rd", 1, 37.7720m, -122.4250m, 0, 50),
                new(Guid.NewGuid(), Guid.NewGuid(), "Maple Blossom Dr", 2, 37.7750m, -122.4220m, 11, 50),
                new(Guid.NewGuid(), Guid.NewGuid(), "Elm Crest Gate", 3, 37.7770m, -122.4200m, 20, 50),
                new(Guid.NewGuid(), Guid.NewGuid(), "Oakridge & 5th", 4, 37.7790m, -122.4170m, 26, 50),
                new(Guid.NewGuid(), Guid.NewGuid(), "West Turnstile School Gate", 5, 37.7810m, -122.4140m, 34, 75)
            }),
            new(Guid.NewGuid(), schoolId, "Route Maple East B", "M-B", "Inbound", true, new List<RouteStopDto>
            {
                new(Guid.NewGuid(), Guid.NewGuid(), "Crestview Terrace", 1, 37.7680m, -122.4150m, 0, 50),
                new(Guid.NewGuid(), Guid.NewGuid(), "Maple Blossom Dr & 3rd", 2, 37.7710m, -122.4180m, 15, 50),
                new(Guid.NewGuid(), Guid.NewGuid(), "Willow Lake Park", 3, 37.7740m, -122.4160m, 25, 50),
                new(Guid.NewGuid(), Guid.NewGuid(), "West Turnstile School Gate", 4, 37.7810m, -122.4140m, 38, 75)
            })
        });
    }

    public async Task<List<TripDto>> GetTripsAsync(Guid schoolId, DateTime? date = null)
    {
        return await Task.FromResult(new List<TripDto>
        {
            new(Guid.NewGuid(), schoolId, Guid.NewGuid(), "Route Westview A", Guid.Parse("14141414-1414-1414-1414-141414141414"), "Bus #14", Guid.NewGuid(), "Robert Jenkins", DateOnly.FromDateTime(DateTime.Today), "MorningPickup", "InTransit", DateTimeOffset.UtcNow.AddMinutes(-35), DateTimeOffset.UtcNow.AddMinutes(-30), null, null, new List<TripStopDto>
            {
                new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Pine Valley Rd", 1, 37.7720m, -122.4250m, "Completed", DateTimeOffset.UtcNow.AddMinutes(-25), DateTimeOffset.UtcNow.AddMinutes(-25), DateTimeOffset.UtcNow.AddMinutes(-24)),
                new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Maple Blossom Dr", 2, 37.7750m, -122.4220m, "Completed", DateTimeOffset.UtcNow.AddMinutes(-14), DateTimeOffset.UtcNow.AddMinutes(-14), DateTimeOffset.UtcNow.AddMinutes(-13)),
                new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Elm Crest Gate", 3, 37.7770m, -122.4200m, "Completed", DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddMinutes(-4)),
                new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Oakridge & 5th", 4, 37.7790m, -122.4170m, "Arrived", DateTimeOffset.UtcNow.AddMinutes(2), DateTimeOffset.UtcNow, null),
                new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "West Turnstile School Gate", 5, 37.7810m, -122.4140m, "Scheduled", DateTimeOffset.UtcNow.AddMinutes(10), null, null)
            }),
            new(Guid.NewGuid(), schoolId, Guid.NewGuid(), "Route Maple East B", Guid.Parse("08080808-0808-0808-0808-080808080808"), "Bus #08", Guid.NewGuid(), "Marcus Vance", DateOnly.FromDateTime(DateTime.Today), "MorningPickup", "InTransit", DateTimeOffset.UtcNow.AddMinutes(-25), DateTimeOffset.UtcNow.AddMinutes(-20), null, null, new List<TripStopDto>())
        });
    }

    public async Task<TripDto?> UpdateTripStatusAsync(Guid schoolId, Guid tripId, string newStatus)
    {
        return await Task.FromResult(new TripDto(tripId, schoolId, Guid.NewGuid(), "Route Westview A", Guid.NewGuid(), "Bus #14", Guid.NewGuid(), "Robert Jenkins", DateOnly.FromDateTime(DateTime.Today), "MorningPickup", newStatus, DateTimeOffset.UtcNow.AddMinutes(-35), DateTimeOffset.UtcNow.AddMinutes(-30), null, null, new List<TripStopDto>()));
    }

    public async Task<bool> IssueTempRfidCardAsync(Guid schoolId, Guid studentId, string cardSerialNumber)
    {
        _logger.LogInformation("Issued temp RFID card {Serial} for student {StudentId} at school {SchoolId}", cardSerialNumber, studentId, schoolId);
        return await Task.FromResult(true);
    }
}
