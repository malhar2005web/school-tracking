using Microsoft.EntityFrameworkCore;
using SchoolTrack.Application.Features.Classes;
using SchoolTrack.Application.Features.Devices;
using SchoolTrack.Application.Features.Drivers;
using SchoolTrack.Application.Features.Fleet;
using SchoolTrack.Application.Features.Parents;
using SchoolTrack.Application.Features.Students;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Infrastructure.Authentication;
using SchoolTrack.Infrastructure.Persistence.Context;
using SchoolTrack.Infrastructure.Services;
using SchoolTrack.Shared.Constants;

namespace SchoolTrack.Api.Tests;

/// <summary>
/// Verifies core school management: students, classes, parents, drivers, buses, RFID cards, and devices.
/// </summary>
public sealed class CoreSchoolManagementTests
{
    private readonly IDateTimeProvider _dateTimeProvider = new SystemDateTimeProvider();
    private readonly PasswordHasher _passwordHasher = new();

    private SchoolTrackDbContext CreateDbContext(string dbName, Guid? schoolId = null, bool isPlatformAdmin = false)
    {
        var options = new DbContextOptionsBuilder<SchoolTrackDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var tenantContext = new StubTenant(schoolId, isPlatformAdmin);
        return new SchoolTrackDbContext(options, tenantContext, _dateTimeProvider);
    }

    [Fact]
    public async Task CreateClass_DuplicateClass_ThrowsArgumentException()
    {
        var dbName = Guid.NewGuid().ToString();
        var schoolId = Guid.NewGuid();

        await using var context = CreateDbContext(dbName, schoolId);
        var tenantContext = new StubTenant(schoolId, false);
        var handler = new CreateClassCommandHandler(context, tenantContext);

        await handler.HandleAsync(new CreateClassCommand("Grade 4", "A", "2026-2027"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(new CreateClassCommand("Grade 4", "A", "2026-2027")));
    }

    [Fact]
    public async Task CreateStudent_UniqueAdmissionNumberPerSchool_Enforced()
    {
        var dbName = Guid.NewGuid().ToString();
        var schoolAId = Guid.NewGuid();
        var schoolBId = Guid.NewGuid();
        var classAId = Guid.NewGuid();
        var classBId = Guid.NewGuid();

        // Seed classes in School A and School B
        await using (var seedContext = CreateDbContext(dbName, null, isPlatformAdmin: true))
        {
            seedContext.Classes.AddRange(
                new Class { Id = classAId, SchoolId = schoolAId, Grade = "Grade 5", Division = "A", AcademicYear = "2026" },
                new Class { Id = classBId, SchoolId = schoolBId, Grade = "Grade 5", Division = "A", AcademicYear = "2026" });
            await seedContext.SaveChangesAsync();
        }

        // 1. Create student in School A with ADM-001
        await using (var contextA = CreateDbContext(dbName, schoolAId))
        {
            var handlerA = new CreateStudentCommandHandler(contextA, new StubTenant(schoolAId, false));
            var studentA = await handlerA.HandleAsync(new CreateStudentCommand(
                classAId, "ADM-001", "Emma", "Watson"));
            Assert.NotNull(studentA);
            Assert.Equal("ADM-001", studentA.AdmissionNumber);

            // Duplicate admission number in School A must fail
            await Assert.ThrowsAsync<ArgumentException>(() =>
                handlerA.HandleAsync(new CreateStudentCommand(classAId, "ADM-001", "Duplicate", "Student")));
        }

        // 2. Same admission number in School B must succeed (tenant isolation)
        await using (var contextB = CreateDbContext(dbName, schoolBId))
        {
            var handlerB = new CreateStudentCommandHandler(contextB, new StubTenant(schoolBId, false));
            var studentB = await handlerB.HandleAsync(new CreateStudentCommand(
                classBId, "ADM-001", "Harry", "Potter"));
            Assert.NotNull(studentB);
            Assert.Equal("ADM-001", studentB.AdmissionNumber);
        }
    }

    [Fact]
    public async Task ParentAndStudentLink_CreatesRelationshipWithGuardianAuthority()
    {
        var dbName = Guid.NewGuid().ToString();
        var schoolId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(dbName, schoolId, isPlatformAdmin: true))
        {
            seedContext.Classes.Add(new Class { Id = classId, SchoolId = schoolId, Grade = "Grade 1", Division = "C", AcademicYear = "2026" });
            seedContext.Students.Add(new Student
            {
                Id = studentId,
                SchoolId = schoolId,
                ClassId = classId,
                AdmissionNumber = "STU-100",
                FirstName = "Leo",
                LastName = "Messi"
            });
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateDbContext(dbName, schoolId);
        var tenantContext = new StubTenant(schoolId, false);

        var createParentHandler = new CreateParentCommandHandler(context, _passwordHasher, tenantContext);
        var parentDto = await createParentHandler.HandleAsync(new CreateParentCommand(
            "Jorge Messi", "jorge@parent.com", "ParentPass123!", "+1-555-0188"));

        Assert.NotNull(parentDto);
        Assert.Equal("jorge@parent.com", parentDto.Email);

        var linkHandler = new LinkStudentCommandHandler(context, tenantContext);
        await linkHandler.HandleAsync(new LinkStudentCommand(
            parentDto.Id, studentId, "Father", IsPrimaryGuardian: true, CanAuthorizePickup: true));

        var linkInDb = await context.ParentStudents.FirstOrDefaultAsync(ps => ps.ParentId == parentDto.Id && ps.StudentId == studentId);
        Assert.NotNull(linkInDb);
        Assert.True(linkInDb!.IsPrimaryGuardian);
        Assert.True(linkInDb.CanAuthorizePickup);

        // Duplicate link should throw
        await Assert.ThrowsAsync<ArgumentException>(() =>
            linkHandler.HandleAsync(new LinkStudentCommand(parentDto.Id, studentId, "Father")));
    }

    [Fact]
    public async Task BusAndDriverAssignment_RotatesActiveDriverProperly()
    {
        var dbName = Guid.NewGuid().ToString();
        var schoolId = Guid.NewGuid();
        var busId = Guid.NewGuid();
        var driver1Id = Guid.NewGuid();
        var driver2Id = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(dbName, schoolId, isPlatformAdmin: true))
        {
            seedContext.Buses.Add(new Bus
            {
                Id = busId,
                SchoolId = schoolId,
                BusNumber = "Bus #07",
                RegistrationNumber = "NY-BUS-07",
                Capacity = 40
            });
            seedContext.Drivers.AddRange(
                new Driver { Id = driver1Id, SchoolId = schoolId, UserId = Guid.NewGuid(), LicenseNumber = "DL-001", Phone = "555-1111" },
                new Driver { Id = driver2Id, SchoolId = schoolId, UserId = Guid.NewGuid(), LicenseNumber = "DL-002", Phone = "555-2222" });
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateDbContext(dbName, schoolId);
        var tenantContext = new StubTenant(schoolId, false);
        var assignHandler = new AssignDriverCommandHandler(context, tenantContext, _dateTimeProvider);

        // Assign Driver 1
        await assignHandler.HandleAsync(new AssignDriverCommand(busId, driver1Id));

        var active1 = await context.BusAssignments.FirstOrDefaultAsync(ba => ba.BusId == busId && ba.IsCurrent);
        Assert.NotNull(active1);
        Assert.Equal(driver1Id, active1!.DriverId);

        // Assign Driver 2 -> Driver 1 must be closed
        await assignHandler.HandleAsync(new AssignDriverCommand(busId, driver2Id));

        var previous = await context.BusAssignments.FirstOrDefaultAsync(ba => ba.BusId == busId && ba.DriverId == driver1Id);
        Assert.False(previous!.IsCurrent);
        Assert.NotNull(previous.AssignedTo);

        var active2 = await context.BusAssignments.FirstOrDefaultAsync(ba => ba.BusId == busId && ba.IsCurrent);
        Assert.NotNull(active2);
        Assert.Equal(driver2Id, active2!.DriverId);
    }

    [Fact]
    public async Task RfidCard_CreationAndAssignmentToStudent_WorksCleanly()
    {
        var dbName = Guid.NewGuid().ToString();
        var schoolId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var classId = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(dbName, schoolId, isPlatformAdmin: true))
        {
            seedContext.Classes.Add(new Class { Id = classId, SchoolId = schoolId, Grade = "Grade 2", Division = "B", AcademicYear = "2026" });
            seedContext.Students.Add(new Student
            {
                Id = studentId,
                SchoolId = schoolId,
                ClassId = classId,
                AdmissionNumber = "ADM-888",
                FirstName = "Luka",
                LastName = "Modric"
            });
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateDbContext(dbName, schoolId);
        var tenantContext = new StubTenant(schoolId, false);

        var createCardHandler = new CreateRfidCardCommandHandler(context, tenantContext, _dateTimeProvider);
        var cardDto = await createCardHandler.HandleAsync(new CreateRfidCardCommand("RFID-E0041234"));

        Assert.NotNull(cardDto);
        Assert.Equal("RFID-E0041234", cardDto.CardUid);
        Assert.Equal("Unassigned", cardDto.Status);

        // Duplicate card UID should throw
        await Assert.ThrowsAsync<ArgumentException>(() =>
            createCardHandler.HandleAsync(new CreateRfidCardCommand("RFID-E0041234")));

        // Assign card to student
        var assignHandler = new AssignRfidCardCommandHandler(context, tenantContext);
        await assignHandler.HandleAsync(new AssignRfidCardCommand(cardDto.Id, studentId));

        var cardInDb = await context.RfidCards.FirstOrDefaultAsync(c => c.Id == cardDto.Id);
        Assert.Equal(studentId, cardInDb!.StudentId);
        Assert.Equal("Active", cardInDb.Status);
    }

    [Fact]
    public async Task GpsAndRfidDevice_RegistrationAndBusBinding_Succeeds()
    {
        var dbName = Guid.NewGuid().ToString();
        var schoolId = Guid.NewGuid();
        var busId = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(dbName, schoolId, isPlatformAdmin: true))
        {
            seedContext.Buses.Add(new Bus
            {
                Id = busId,
                SchoolId = schoolId,
                BusNumber = "Bus #22",
                RegistrationNumber = "REG-22",
                Capacity = 32
            });
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateDbContext(dbName, schoolId);
        var tenantContext = new StubTenant(schoolId, false);

        var gpsHandler = new RegisterGpsDeviceCommandHandler(context, tenantContext);
        var gpsDto = await gpsHandler.HandleAsync(new RegisterGpsDeviceCommand(
            "GPS-HW-001", busId, "358901234567890", "Teltonika FMB920"));

        Assert.NotNull(gpsDto);
        Assert.Equal("GPS-HW-001", gpsDto.DeviceCode);
        Assert.Equal("Bus #22", gpsDto.BusNumber);

        var rfidHandler = new RegisterRfidDeviceCommandHandler(context, tenantContext);
        var rfidDto = await rfidHandler.HandleAsync(new RegisterRfidDeviceCommand(
            "RFID-HW-001", busId, GateLocation: "Front Door", Model: "ZKTeco ProID30"));

        Assert.NotNull(rfidDto);
        Assert.Equal("RFID-HW-001", rfidDto.DeviceCode);
        Assert.Equal("Bus #22", rfidDto.BusNumber);

        // Query buses to verify device code links
        var getBusesHandler = new GetBusesQueryHandler(context);
        var buses = await getBusesHandler.HandleAsync(new GetBusesQuery());

        Assert.Single(buses);
        Assert.Equal("GPS-HW-001", buses[0].GpsDeviceCode);
        Assert.Equal("RFID-HW-001", buses[0].RfidDeviceCode);
    }

    [Fact]
    public async Task TenantIsolation_SchoolAdmin_CannotSeeOtherSchoolStudentsOrBuses()
    {
        var dbName = Guid.NewGuid().ToString();
        var schoolAId = Guid.NewGuid();
        var schoolBId = Guid.NewGuid();
        var classAId = Guid.NewGuid();
        var classBId = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(dbName, null, isPlatformAdmin: true))
        {
            seedContext.Classes.AddRange(
                new Class { Id = classAId, SchoolId = schoolAId, Grade = "1", Division = "A", AcademicYear = "2026" },
                new Class { Id = classBId, SchoolId = schoolBId, Grade = "1", Division = "A", AcademicYear = "2026" });

            seedContext.Students.AddRange(
                new Student { Id = Guid.NewGuid(), SchoolId = schoolAId, ClassId = classAId, AdmissionNumber = "A-1", FirstName = "Alice", LastName = "A" },
                new Student { Id = Guid.NewGuid(), SchoolId = schoolBId, ClassId = classBId, AdmissionNumber = "B-1", FirstName = "Bob", LastName = "B" });

            seedContext.Buses.AddRange(
                new Bus { Id = Guid.NewGuid(), SchoolId = schoolAId, BusNumber = "Bus A", RegistrationNumber = "A-BUS" },
                new Bus { Id = Guid.NewGuid(), SchoolId = schoolBId, BusNumber = "Bus B", RegistrationNumber = "B-BUS" });

            await seedContext.SaveChangesAsync();
        }

        // Query as School A Admin
        await using (var contextA = CreateDbContext(dbName, schoolAId, isPlatformAdmin: false))
        {
            var studentHandler = new GetStudentsQueryHandler(contextA);
            var students = await studentHandler.HandleAsync(new GetStudentsQuery());

            Assert.Single(students);
            Assert.Equal("A-1", students[0].AdmissionNumber);

            var busHandler = new GetBusesQueryHandler(contextA);
            var buses = await busHandler.HandleAsync(new GetBusesQuery());

            Assert.Single(buses);
            Assert.Equal("Bus A", buses[0].BusNumber);
        }
    }

    private sealed class StubTenant(Guid? schoolId, bool isPlatformAdmin) : ITenantContext
    {
        public Guid? SchoolId => schoolId;
        public bool IsPlatformAdmin => isPlatformAdmin;
    }
}

