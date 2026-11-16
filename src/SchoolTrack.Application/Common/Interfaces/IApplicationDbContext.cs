using Microsoft.EntityFrameworkCore;
using SchoolTrack.Domain.Entities;

namespace SchoolTrack.Application.Common.Interfaces;

/// <summary>
/// Exposes entity sets and transactional persistence without binding to the underlying ORM provider.
/// </summary>
public interface IApplicationDbContext : IUnitOfWork
{
    DbSet<School> Schools { get; }
    DbSet<SchoolSettings> SchoolSettings { get; }
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<AuditLog> AuditLogs { get; }

    DbSet<Class> Classes { get; }
    DbSet<Student> Students { get; }
    DbSet<Parent> Parents { get; }
    DbSet<ParentStudent> ParentStudents { get; }
    DbSet<Bus> Buses { get; }
    DbSet<Driver> Drivers { get; }
    DbSet<BusAssignment> BusAssignments { get; }
    DbSet<RfidCard> RfidCards { get; }
    DbSet<GpsDevice> GpsDevices { get; }
    DbSet<RfidDevice> RfidDevices { get; }

    DbSet<Route> Routes { get; }
    DbSet<RouteStop> RouteStops { get; }
    DbSet<RouteAssignment> RouteAssignments { get; }
    DbSet<Trip> Trips { get; }
    DbSet<TripStop> TripStops { get; }
    DbSet<TripStatusHistory> TripStatusHistories { get; }

    DbSet<LocationLog> LocationLogs { get; }
    DbSet<LatestBusLocation> LatestBusLocations { get; }

    DbSet<BoardingEvent> BoardingEvents { get; }
    DbSet<UnknownCardTap> UnknownCardTaps { get; }
}
