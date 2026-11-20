using Microsoft.Extensions.Logging;
using SchoolTrack.ParentApp.Services;
using SchoolTrack.DriverApp.Services;
using SchoolTrack.Admin.Services;

namespace SchoolTrack.ParentApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // HTTP Client
        builder.Services.AddScoped(sp => new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000/")
        });

        // 1. Parent App Services
        builder.Services.AddSingleton<ParentAuthState>();
        builder.Services.AddSingleton<IParentApiClient, ParentApiClient>();
        builder.Services.AddSingleton<ParentTrackingHubService>();
        builder.Services.AddSingleton<IParentNotificationService, ParentNotificationService>();

        // 2. Driver App Services
        builder.Services.AddSingleton<DriverAuthState>();
        builder.Services.AddSingleton<IDriverApiClient, DriverApiClient>();
        builder.Services.AddSingleton<DriverTrackingHubService>();
        builder.Services.AddSingleton<IDriverNotificationService, DriverNotificationService>();

        // 3. Admin Portal Services
        builder.Services.AddSingleton<AdminAuthStateProvider>();
        builder.Services.AddSingleton<IApiClient, ApiClient>();
        builder.Services.AddSingleton<LiveFleetTrackingService>();

        return builder.Build();
    }
}
