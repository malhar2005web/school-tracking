using SchoolTrack.DriverApp.Models;

namespace SchoolTrack.DriverApp.Services;

public interface IDriverApiClient
{
    Task<DriverSession?> LoginAsync(string licenseNumber, string pin);
    Task<DriverActiveRouteModel?> GetActiveRouteAsync(Guid busId, string shift = "Morning");
    Task<List<DriverManifestStudentModel>> GetStudentManifestAsync(Guid tripId, string shift = "Morning");
    Task<bool> ManualCheckInStudentAsync(Guid tripId, Guid studentId, string reason);
    Task<bool> ManualDeboardStudentAsync(Guid tripId, Guid studentId, string reason);
    Task<bool> MarkStudentAbsentAsync(Guid tripId, Guid studentId, string reason);
    Task<bool> CompleteStopAsync(Guid tripId, Guid stopId);
    Task<bool> CompleteTripAsync(Guid tripId);
    Task<bool> BroadcastDelayAsync(Guid tripId, BroadcastDelayRequest request);
    Task<bool> SubmitPreTripChecklistAsync(Guid busId, DriverPreTripChecklist checklist);
    Task<bool> TriggerEmergencySosAsync(DriverSosAlertRequest request);
}

