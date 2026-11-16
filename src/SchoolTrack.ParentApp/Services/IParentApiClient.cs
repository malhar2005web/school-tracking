using SchoolTrack.ParentApp.Models;

namespace SchoolTrack.ParentApp.Services;

public interface IParentApiClient
{
    Task<ParentSession?> LoginWithPhoneAsync(string phoneNumber, string otpCode);
    Task<ParentSession?> LoginWithEmailAsync(string email, string password);
    Task<ChildActiveTripModel?> GetChildActiveTripAsync(Guid studentId, string shift = "Morning");
    Task<List<RfidTransitLogModel>> GetChildRfidLogsAsync(Guid studentId, DateTime date);
    Task<bool> ReportAbsenceAsync(AbsenceReportRequest request);
    Task<ParentSafetySettings> GetSafetySettingsAsync(Guid studentId);
    Task<bool> SaveSafetySettingsAsync(Guid studentId, ParentSafetySettings settings);
    Task<List<ParentNotificationModel>> GetNotificationsAsync();
    Task<bool> MarkNotificationReadAsync(Guid notificationId);
    Task<bool> TriggerEmergencyAlertAsync(Guid studentId, string reason);
    Task<List<AuthorizedGuardianModel>> GetAuthorizedGuardiansAsync(Guid studentId);
    Task<bool> CloseParentTripAsync(Guid studentId);
    Task<bool> ToggleSimulationChildBoardingStateAsync(Guid studentId, string nextState);
}

