using Microsoft.Extensions.Logging;
using SchoolTrack.ParentApp.Services;

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

        // SchoolTrack Parent App Services
        builder.Services.AddSingleton<ParentAuthState>();
        builder.Services.AddSingleton<IParentApiClient, ParentApiClient>();
        builder.Services.AddSingleton<ParentTrackingHubService>();
        builder.Services.AddSingleton<IParentNotificationService, ParentNotificationService>();

        return builder.Build();
    }
}
