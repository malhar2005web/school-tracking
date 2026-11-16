using Microsoft.Extensions.DependencyInjection;
using SchoolTrack.DeviceGateway.Abstractions;
using SchoolTrack.DeviceGateway.Adapters;
using SchoolTrack.DeviceGateway.Geofencing;
using SchoolTrack.DeviceGateway.Services;
using SchoolTrack.DeviceGateway.Simulator;
using SchoolTrack.DeviceGateway.Validation;

namespace SchoolTrack.DeviceGateway;

/// <summary>
/// Registers protocol-agnostic RFID and GPS device gateway services, protocol adapters, and simulators.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds SchoolTrack Device Gateway services to the service collection.
    /// </summary>
    public static IServiceCollection AddDeviceGateway(this IServiceCollection services)
    {
        services.AddSingleton<IGpsProtocolAdapter, HttpJsonGpsAdapter>();
        services.AddSingleton<IRfidProtocolAdapter, HttpJsonRfidAdapter>();
        services.AddSingleton<IGeofenceEvaluator, GeofenceEvaluator>();
        services.AddScoped<IGpsValidationService, GpsValidationService>();
        services.AddScoped<IGpsIngestionService, GpsIngestionService>();
        services.AddScoped<IGpsSimulatorService, GpsSimulatorService>();
        services.AddScoped<IAdvancedGpsSimulator, AdvancedGpsSimulator>();
        services.AddScoped<IRfidIngestionService, RfidIngestionService>();
        services.AddScoped<IRfidSimulatorService, RfidSimulatorService>();
        services.AddScoped<IAdvancedRfidSimulator, AdvancedRfidSimulator>();

        return services;
    }
}
