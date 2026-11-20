using SchoolTrack.DriverApp.Models;

namespace SchoolTrack.DriverApp.Services;

public class DriverAuthState
{
    private DriverSession? _session;

    public event Action? OnChange;

    public bool IsAuthenticated => _session != null && !string.IsNullOrWhiteSpace(_session.AccessToken);

    public DriverSession? CurrentSession => _session;

    public Guid? ActiveTripId { get; private set; }

    public string CurrentShift { get; private set; } = "Morning";

    public bool IsInTransit => _session?.ShiftStatus == "InTransit";

    public bool IsCompleted => _session?.ShiftStatus == "Completed";

    public DriverAuthState()
    {
        InitializeDefaultDemoSession();
    }

    public void InitializeDefaultDemoSession()
    {
        var schoolId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var driverId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var busId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var routeId = Guid.Parse("88888888-8888-8888-8888-888888888888");

        _session = new DriverSession(
            DriverId: driverId,
            SchoolId: schoolId,
            FullName: "Robert Jenkins",
            LicenseNumber: "CDL-90824-A",
            PhoneNumber: "+1 (555) 329-8472",
            AvatarUrl: "https://lh3.googleusercontent.com/aida-public/AB6AXuAFrPPwe8Q228TD2qqumI7mbdRj2huhzXJ4IIeNLkiVyNwREAD3fXB8WR9UWI-f8oqn-DfnM14NsLr_fA8gCMOJ7DyskjIurGpVIpCCrZMohsoTpDMxaSHZuKEm3D4xBsoHZz1e8tkLGYnm8lCsOnMbHDKIngoN98ofTTAb1SYpTPusgVlhuXGmx9FQ0fUtpb6TsaDZGIjALJusVxxi2cofTOGO4YO2J4TES6BjmVM",
            BusId: busId,
            BusNumber: "Bus #14",
            RouteId: routeId,
            RouteName: "Morning Route 14-AM • Oakridge East",
            AccessToken: "demo-driver-jwt-token-robert",
            ShiftStatus: "InTransit"
        );

        ActiveTripId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    }

    public void SetSession(DriverSession session)
    {
        _session = session;
        ActiveTripId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        NotifyStateChanged();
    }

    public void StartTrip()
    {
        if (_session != null)
        {
            _session = _session with { ShiftStatus = "InTransit" };
            NotifyStateChanged();
        }
    }

    public void CompleteTrip()
    {
        if (_session != null)
        {
            _session = _session with { ShiftStatus = "Completed" };
            NotifyStateChanged();
        }
    }

    public void SwitchShift(string shift)
    {
        CurrentShift = shift;
        if (_session != null)
        {
            var isEvening = shift.Equals("Evening", StringComparison.OrdinalIgnoreCase) || shift.Equals("Afternoon", StringComparison.OrdinalIgnoreCase);
            var routeName = isEvening
                ? "Evening Route 14-PM • Oakridge to Neighborhoods"
                : "Morning Route 14-AM • Neighborhoods to Oakridge";
            var tripId = isEvening
                ? Guid.Parse("33333333-3333-3333-3333-333333333334")
                : Guid.Parse("33333333-3333-3333-3333-333333333333");

            _session = _session with
            {
                RouteName = routeName,
                ShiftStatus = "InTransit"
            };
            ActiveTripId = tripId;
            NotifyStateChanged();
        }
    }

    public void ResetTrip()
    {
        if (_session != null)
        {
            _session = _session with { ShiftStatus = "InTransit" };
            NotifyStateChanged();
        }
    }

    public void Logout()
    {
        _session = null;
        ActiveTripId = null;
        NotifyStateChanged();
    }

    public void NotifyStateChanged() => OnChange?.Invoke();
}

