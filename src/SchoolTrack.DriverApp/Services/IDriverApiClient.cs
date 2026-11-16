using SchoolTrack.DriverApp.Models;

namespace SchoolTrack.DriverApp.Services;

public interface IDriverApiClient
{
    Task<DriverSession?> LoginAsync(string licenseNumber, string pin);
    Task<DriverActiveRouteModel?> GetActiveRouteAsync(Guid busId);
    Task<List<DriverManifestStudentModel>> GetStudentManifestAsync(Guid tripId);
    Task<bool> ManualCheckInStudentAsync(Guid tripId, Guid studentId, string reason);
    Task<bool> MarkStudentAbsentAsync(Guid tripId, Guid studentId, string reason);
    Task<bool> CompleteStopAsync(Guid tripId, Guid stopId);
    Task<bool> BroadcastDelayAsync(Guid tripId, BroadcastDelayRequest request);
    Task<bool> SubmitPreTripChecklistAsync(Guid busId, DriverPreTripChecklist checklist);
    Task<bool> TriggerEmergencySosAsync(DriverSosAlertRequest request);
}

