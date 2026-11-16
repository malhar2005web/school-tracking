using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SchoolTrack.Application.Features.Auth.Commands.Login;
using SchoolTrack.Application.Features.Auth.Commands.Logout;
using SchoolTrack.Application.Features.Auth.Commands.RefreshToken;
using SchoolTrack.Application.Features.Auth.Queries.GetCurrentUser;
using SchoolTrack.Application.Features.Classes;
using SchoolTrack.Application.Features.Devices;
using SchoolTrack.Application.Features.Drivers;
using SchoolTrack.Application.Features.Fleet;
using SchoolTrack.Application.Features.Parents;
using SchoolTrack.Application.Features.Routes;
using SchoolTrack.Application.Features.Schools.Commands.CreateSchool;
using SchoolTrack.Application.Features.Schools.Queries.GetSchoolById;
using SchoolTrack.Application.Features.Schools.Queries.GetSchools;
using SchoolTrack.Application.Features.Students;
using SchoolTrack.Application.Features.Trips;

namespace SchoolTrack.Application;

/// <summary>
/// Registers application layer services, validators, and use-case handlers.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds SchoolTrack Application layer dependencies to the service collection.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Auth handlers
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<RefreshTokenCommandHandler>();
        services.AddScoped<LogoutCommandHandler>();
        services.AddScoped<GetCurrentUserQueryHandler>();

        // School handlers
        services.AddScoped<CreateSchoolCommandHandler>();
        services.AddScoped<GetSchoolsQueryHandler>();
        services.AddScoped<GetSchoolByIdQueryHandler>();

        // Class handlers
        services.AddScoped<CreateClassCommandHandler>();
        services.AddScoped<GetClassesQueryHandler>();

        // Student handlers
        services.AddScoped<CreateStudentCommandHandler>();
        services.AddScoped<UpdateStudentCommandHandler>();
        services.AddScoped<GetStudentsQueryHandler>();
        services.AddScoped<GetStudentByIdQueryHandler>();

        // Parent handlers
        services.AddScoped<CreateParentCommandHandler>();
        services.AddScoped<LinkStudentCommandHandler>();
        services.AddScoped<GetParentsQueryHandler>();

        // Driver handlers
        services.AddScoped<CreateDriverCommandHandler>();
        services.AddScoped<GetDriversQueryHandler>();

        // Fleet handlers
        services.AddScoped<CreateBusCommandHandler>();
        services.AddScoped<AssignDriverCommandHandler>();
        services.AddScoped<GetBusesQueryHandler>();

        // Device handlers
        services.AddScoped<CreateRfidCardCommandHandler>();
        services.AddScoped<AssignRfidCardCommandHandler>();
        services.AddScoped<RegisterGpsDeviceCommandHandler>();
        services.AddScoped<RegisterRfidDeviceCommandHandler>();
        services.AddScoped<GetRfidCardsQueryHandler>();
        services.AddScoped<GetGpsDevicesQueryHandler>();
        services.AddScoped<GetRfidDevicesQueryHandler>();

        // Route handlers
        services.AddScoped<CreateRouteCommandHandler>();
        services.AddScoped<AddRouteStopCommandHandler>();
        services.AddScoped<CreateRouteAssignmentCommandHandler>();
        services.AddScoped<GetRoutesQueryHandler>();
        services.AddScoped<GetRouteByIdQueryHandler>();

        // Trip handlers
        services.AddScoped<CreateTripCommandHandler>();
        services.AddScoped<UpdateTripStatusCommandHandler>();
        services.AddScoped<UpdateTripStopStatusCommandHandler>();
        services.AddScoped<GetTripsQueryHandler>();
        services.AddScoped<GetTripByIdQueryHandler>();

        // Telemetry handlers
        services.AddScoped<Features.Telemetry.GetLatestBusLocationQueryHandler>();
        services.AddScoped<Features.Telemetry.GetTripTrailQueryHandler>();

        // Boarding handlers
        services.AddScoped<Features.Boarding.GetTripPassengerManifestQueryHandler>();
        services.AddScoped<Features.Boarding.GetUnknownCardTapsQueryHandler>();

        return services;
    }
}
