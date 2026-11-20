using SchoolTrack.Admin.Models;
using SchoolTrack.Shared.DTOs.Auth;
using SchoolTrack.Shared.DTOs.Drivers;
using SchoolTrack.Shared.DTOs.Fleet;
using SchoolTrack.Shared.DTOs.Routes;
using SchoolTrack.Shared.DTOs.Schools;
using SchoolTrack.Shared.DTOs.Students;
using SchoolTrack.Shared.DTOs.Trips;

namespace SchoolTrack.Admin.Services;

public interface IApiClient
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<List<SchoolDto>> GetSchoolsAsync();
    Task<DashboardSummaryViewModel> GetDashboardSummaryAsync(Guid schoolId);
    Task<List<BusLiveStatusItem>> GetFleetRadarAsync(Guid schoolId);
    Task<List<StudentDto>> GetStudentsAsync(Guid schoolId, string? grade = null, string? search = null);
    Task<StudentDto?> CreateStudentAsync(Guid schoolId, CreateStudentRequest request);
    Task<List<BusDto>> GetBusesAsync(Guid schoolId);
    Task<List<DriverDto>> GetDriversAsync(Guid schoolId);
    Task<List<RouteDto>> GetRoutesAsync(Guid schoolId);
    Task<List<TripDto>> GetTripsAsync(Guid schoolId, DateTime? date = null);
    Task<TripDto?> UpdateTripStatusAsync(Guid schoolId, Guid tripId, string newStatus);
    Task<bool> IssueTempRfidCardAsync(Guid schoolId, Guid studentId, string cardSerialNumber);
}
