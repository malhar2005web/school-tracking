using SchoolTrack.Api.Extensions;
using SchoolTrack.Api.Hubs;
using SchoolTrack.Api.Middleware;
using SchoolTrack.Api.Services;
using SchoolTrack.Application;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.DeviceGateway;
using SchoolTrack.Infrastructure;
using SchoolTrack.Infrastructure.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext(),
        preserveStaticLogger: true);

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddDeviceGateway();

    builder.Services.AddSchoolTrackAuthentication(builder.Configuration);
    builder.Services.AddSchoolTrackAuthorization();
    builder.Services.AddSchoolTrackCors(builder.Configuration);
    builder.Services.AddSchoolTrackRateLimiting();
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
    });

    builder.Services.AddSignalR();
    builder.Services.AddSingleton<ITrackingBroadcaster, SignalRTrackingBroadcaster>();
    builder.Services.AddHostedService<BusHeartbeatMonitorHostedService>();

    builder.Services.AddControllers();
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddSchoolTrackSwagger();
    builder.Services.AddSchoolTrackHealthChecks(builder.Configuration);

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseSchoolTrackSecurityHeaders();
    app.UseResponseCompression();
    app.UseSerilogRequestLogging();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseCors(CorsExtensions.DefaultCorsPolicy);
    app.UseRateLimiter();
    app.UseSchoolTrackSwagger(builder.Configuration);
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<TrackingHub>("/hubs/tracking");
    app.MapSchoolTrackHealthChecks();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "SchoolTrack API terminated unexpectedly during startup.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Partial entry point declaration exposed for ASP.NET Core integration testing.
/// </summary>
public partial class Program;
