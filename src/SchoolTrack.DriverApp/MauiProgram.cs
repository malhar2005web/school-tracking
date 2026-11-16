using Microsoft.Extensions.Logging;
using SchoolTrack.DriverApp.Services;

namespace SchoolTrack.DriverApp;

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

        // SchoolTrack Driver App Services
        builder.Services.AddSingleton<DriverAuthState>();
        builder.Services.AddSingleton<IDriverApiClient, DriverApiClient>();
        builder.Services.AddSingleton<DriverTrackingHubService>();
        builder.Services.AddSingleton<IDriverNotificationService, DriverNotificationService>();

        return builder.Build();
    }
}
