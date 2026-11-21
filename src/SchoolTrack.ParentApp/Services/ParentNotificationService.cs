using SchoolTrack.ParentApp.Models;

namespace SchoolTrack.ParentApp.Services;

public interface IParentNotificationService
{
    event Action<ParentNotificationModel>? OnNotificationReceived;
    IReadOnlyList<ParentNotificationModel> ActiveToasts { get; }
    void ShowToast(string title, string message, string type = "Boarding", string childName = "Leo Martinez", string? actionLabel = null, string? actionTarget = null);
    void DismissToast(Guid id);
}

public class ParentNotificationService : IParentNotificationService
{
    private readonly List<ParentNotificationModel> _activeToasts = new();

    public event Action<ParentNotificationModel>? OnNotificationReceived;

    public IReadOnlyList<ParentNotificationModel> ActiveToasts => _activeToasts.AsReadOnly();

    public void ShowToast(string title, string message, string type = "Boarding", string childName = "Leo Martinez", string? actionLabel = null, string? actionTarget = null)
    {
        var toast = new ParentNotificationModel(
            Id: Guid.NewGuid(),
            Title: title,
            Message: message,
            Timestamp: DateTimeOffset.UtcNow,
            Type: type,
            IsRead: false,
            ChildName: childName,
            ActionLabel: actionLabel,
            ActionTarget: actionTarget
        );

        _activeToasts.Insert(0, toast);

        // Auto-dismiss after 7 seconds
        _ = Task.Run(async () =>
        {
            await Task.Delay(7000);
            DismissToast(toast.Id);
        });

        OnNotificationReceived?.Invoke(toast);
    }

    public void DismissToast(Guid id)
    {
        var match = _activeToasts.FirstOrDefault(t => t.Id == id);
        if (match != null)
        {
            _activeToasts.Remove(match);
            OnNotificationReceived?.Invoke(match);
        }
    }
}

