namespace SchoolTrack.DriverApp.Services;

public record DriverToastModel(Guid Id, string Title, string Message, string Type, DateTimeOffset Timestamp);

public interface IDriverNotificationService
{
    event Action<DriverToastModel>? OnToastReceived;
    IReadOnlyList<DriverToastModel> ActiveToasts { get; }
    void ShowToast(string title, string message, string type = "Info");
    void DismissToast(Guid id);
}

public class DriverNotificationService : IDriverNotificationService
{
    private readonly List<DriverToastModel> _activeToasts = new();

    public event Action<DriverToastModel>? OnToastReceived;

    public IReadOnlyList<DriverToastModel> ActiveToasts => _activeToasts.AsReadOnly();

    public void ShowToast(string title, string message, string type = "Info")
    {
        var toast = new DriverToastModel(
            Id: Guid.NewGuid(),
            Title: title,
            Message: message,
            Type: type,
            Timestamp: DateTimeOffset.UtcNow
        );

        _activeToasts.Insert(0, toast);

        // Auto dismiss after 5 seconds
        _ = Task.Run(async () =>
        {
            await Task.Delay(5000);
            DismissToast(toast.Id);
        });

        OnToastReceived?.Invoke(toast);
    }

    public void DismissToast(Guid id)
    {
        var match = _activeToasts.FirstOrDefault(t => t.Id == id);
        if (match != null)
        {
            _activeToasts.Remove(match);
            OnToastReceived?.Invoke(match);
        }
    }
}

