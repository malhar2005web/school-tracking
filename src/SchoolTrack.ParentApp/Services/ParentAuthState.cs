using SchoolTrack.ParentApp.Models;

namespace SchoolTrack.ParentApp.Services;

public class ParentAuthState
{
    private ParentSession? _session;
    private ChildProfileModel? _activeChild;

    public event Action? OnChange;

    public bool IsAuthenticated => _session != null && !string.IsNullOrWhiteSpace(_session.AccessToken);

    public ParentSession? CurrentSession => _session;

    public ChildProfileModel? ActiveChild => _activeChild;

    public IReadOnlyList<ChildProfileModel> LinkedChildren => _session?.LinkedChildren ?? Array.Empty<ChildProfileModel>();

    public ParentAuthState()
    {
        // Initialize with default demo session matching Stitch UI specifications
        InitializeDefaultDemoSession();
    }

    public void InitializeDefaultDemoSession()
    {
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
            AvatarUrl: "https://lh3.googleusercontent.com/aida-public/AB6AXuD9dKIaQUU9h42sNd8oUEUv43uMcsxluN1p-E8T706D56YLD-sfigov8-XaEzIlPhe9oVk1pI1tsG8GhEgV_CayKJWU9J2Q_WMssM1w3YUIf-d3fs9ExbONRN3qgWUuQVU9oIf4244keIKmANrbecnnPIn7jucmKiNvCB-YFRb3oao93xnf5sE5EmLHBCwtSSGwMmrp0-X1p0_eIfoRphvfmMP4dhSCaXVQo1MYwB4",
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

        _session = new ParentSession(
            ParentUserId: parentId,
            SchoolId: schoolId,
            FullName: "Sarah Martinez",
            Email: "sarah.martinez@example.com",
            PhoneNumber: "+1 (555) 019-2834",
            AccessToken: "demo-parent-token-sarah",
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddDays(7),
            LinkedChildren: new List<ChildProfileModel> { leo, maya }
        );

        _activeChild = leo;
    }

    public void SetSession(ParentSession session)
    {
        _session = session;
        _activeChild = session.LinkedChildren.FirstOrDefault();
        NotifyStateChanged();
    }

    public void SelectChild(Guid studentId)
    {
        if (_session != null)
        {
            var match = _session.LinkedChildren.FirstOrDefault(c => c.StudentId == studentId);
            if (match != null)
            {
                _activeChild = match;
                NotifyStateChanged();
            }
        }
    }

    public void Logout()
    {
        _session = null;
        _activeChild = null;
        NotifyStateChanged();
    }

    public void NotifyStateChanged() => OnChange?.Invoke();
}

