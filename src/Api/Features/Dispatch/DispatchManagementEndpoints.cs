using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Api.Features.ManifestImports;
using Mdsweep.Api.Infrastructure;

namespace Mdsweep.Api.Features.Dispatch;

public static class DispatchManagementEndpoints
{
    public static IEndpointRouteBuilder MapDispatchManagement(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/drivers", ListDrivers).RequireAuthorization();
        endpoints.MapPost("/api/drivers", CreateDriver).RequireAuthorization();
        endpoints.MapPost("/api/drivers/access", CreateDriverAccess).RequireAuthorization();
        endpoints.MapPost("/api/drivers/{driverId:guid}/reset-access", ResetDriverAccess).RequireAuthorization();
        endpoints.MapPost("/api/drivers/{driverId:guid}/deactivate", DeactivateDriver).RequireAuthorization();
        endpoints.MapGet("/api/vehicles", ListVehicles).RequireAuthorization();
        endpoints.MapPost("/api/vehicles", CreateVehicle).RequireAuthorization();
        endpoints.MapPost("/api/vehicles/{vehicleId:guid}/deactivate", DeactivateVehicle).RequireAuthorization();
        endpoints.MapPost("/api/journeys/{journeyKey}/assignments", AssignJourney).RequireAuthorization();
        endpoints.MapPost("/api/trips/{tripNumber}/assignments", AssignTrip).RequireAuthorization();
        endpoints.MapGet("/api/trips/{tripNumber}/assignments", AssignmentHistory).RequireAuthorization();
        endpoints.MapGet("/api/driver-work/assignments", DriverAssignments).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> ListDrivers(ClaimsPrincipal user, ApplicationDbContext db, CancellationToken ct)
    {
        var context = await DispatcherContext(user, db, ct); if (context is null) return Results.Forbid();
        return Results.Ok(await db.Drivers.Where(x => x.ProviderId == context.ProviderId)
            .OrderBy(x => x.DisplayName).Select(x => new DriverResponse(x.Id, x.AppUserId, x.DisplayName, x.MtmDriverNumber, x.IsActive)).ToListAsync(ct));
    }

    private static async Task<IResult> CreateDriver(CreateDriverRequest request, ClaimsPrincipal user, ApplicationDbContext db, CancellationToken ct)
    {
        var context = await DispatcherContext(user, db, ct); if (context is null) return Results.Forbid();
        if (string.IsNullOrWhiteSpace(request.DisplayName) || string.IsNullOrWhiteSpace(request.MtmDriverNumber)) return Results.BadRequest(new { message = "A Driver name and MTM Driver Number are required." });
        var hasMembership = await db.ProviderMemberships.AnyAsync(x => x.ProviderId == context.ProviderId && x.AppUserId == request.AppUserId && x.Role == "Driver", ct);
        if (!hasMembership) return Results.BadRequest(new { message = "The App User must be a Driver for this Provider." });
        var driver = new Driver { ProviderId = context.ProviderId, AppUserId = request.AppUserId, DisplayName = request.DisplayName.Trim(), MtmDriverNumber = request.MtmDriverNumber.Trim() };
        db.Drivers.Add(driver); await db.SaveChangesAsync(ct);
        return Results.Created($"/api/drivers/{driver.Id}", new DriverResponse(driver.Id, driver.AppUserId, driver.DisplayName, driver.MtmDriverNumber, driver.IsActive));
    }

    private static async Task<IResult> CreateDriverAccess(CreateDriverAccessRequest request, ClaimsPrincipal user, ApplicationDbContext db, IKeycloakUserAdministration keycloak, CancellationToken ct)
    {
        var context = await DispatcherContext(user, db, ct); if (context is null) return Results.Forbid();
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.TemporaryPassword) || string.IsNullOrWhiteSpace(request.DisplayName) || string.IsNullOrWhiteSpace(request.MtmDriverNumber)) return Results.BadRequest(new { message = "Email, temporary password, Driver name, and MTM Driver Number are required." });
        var organizationId = await db.Providers.Where(x => x.Id == context.ProviderId).Select(x => x.KeycloakOrganizationId).SingleAsync(ct);
        var subject = await keycloak.CreateDriverAsync(request.Email.Trim(), request.TemporaryPassword, organizationId, ct);
        var appUser = new AppUser { KeycloakSubject = subject };
        var driver = new Driver { ProviderId = context.ProviderId, AppUserId = appUser.Id, DisplayName = request.DisplayName.Trim(), MtmDriverNumber = request.MtmDriverNumber.Trim() };
        db.AppUsers.Add(appUser); db.ProviderMemberships.Add(new ProviderMembership { ProviderId = context.ProviderId, AppUserId = appUser.Id, Role = "Driver" }); db.Drivers.Add(driver);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            await keycloak.DeleteUserAsync(subject, CancellationToken.None);
            throw;
        }
        return Results.Created($"/api/drivers/{driver.Id}", new DriverResponse(driver.Id, appUser.Id, driver.DisplayName, driver.MtmDriverNumber, driver.IsActive));
    }

    private static async Task<IResult> ResetDriverAccess(Guid driverId, ResetDriverAccessRequest request, ClaimsPrincipal user, ApplicationDbContext db, IKeycloakUserAdministration keycloak, CancellationToken ct)
    {
        var context = await DispatcherContext(user, db, ct); if (context is null) return Results.Forbid();
        if (string.IsNullOrWhiteSpace(request.TemporaryPassword)) return Results.BadRequest(new { message = "A temporary password is required." });
        var driver = await db.Drivers.SingleOrDefaultAsync(x => x.Id == driverId && x.ProviderId == context.ProviderId, ct); if (driver is null) return Results.NotFound();
        var subject = await db.AppUsers.Where(x => x.Id == driver.AppUserId).Select(x => x.KeycloakSubject).SingleAsync(ct);
        await keycloak.ResetPasswordAsync(subject, request.TemporaryPassword, ct); return Results.NoContent();
    }

    private static async Task<IResult> DeactivateDriver(Guid driverId, ClaimsPrincipal user, ApplicationDbContext db, CancellationToken ct)
    {
        var context = await DispatcherContext(user, db, ct); if (context is null) return Results.Forbid();
        var driver = await db.Drivers.SingleOrDefaultAsync(x => x.Id == driverId && x.ProviderId == context.ProviderId, ct); if (driver is null) return Results.NotFound();
        driver.IsActive = false; await db.SaveChangesAsync(ct); return Results.NoContent();
    }

    private static async Task<IResult> ListVehicles(ClaimsPrincipal user, ApplicationDbContext db, CancellationToken ct)
    {
        var context = await DispatcherContext(user, db, ct); if (context is null) return Results.Forbid();
        return Results.Ok(await db.Vehicles.Where(x => x.ProviderId == context.ProviderId).OrderBy(x => x.DisplayName)
            .Select(x => new VehicleResponse(x.Id, x.DisplayName, x.Vin, x.IsActive)).ToListAsync(ct));
    }

    private static async Task<IResult> CreateVehicle(CreateVehicleRequest request, ClaimsPrincipal user, ApplicationDbContext db, CancellationToken ct)
    {
        var context = await DispatcherContext(user, db, ct); if (context is null) return Results.Forbid();
        if (string.IsNullOrWhiteSpace(request.DisplayName) || string.IsNullOrWhiteSpace(request.Vin)) return Results.BadRequest(new { message = "A Vehicle name and VIN are required." });
        var vehicle = new Vehicle { ProviderId = context.ProviderId, DisplayName = request.DisplayName.Trim(), Vin = request.Vin.Trim() };
        db.Vehicles.Add(vehicle); await db.SaveChangesAsync(ct);
        return Results.Created($"/api/vehicles/{vehicle.Id}", new VehicleResponse(vehicle.Id, vehicle.DisplayName, vehicle.Vin, vehicle.IsActive));
    }

    private static async Task<IResult> DeactivateVehicle(Guid vehicleId, ClaimsPrincipal user, ApplicationDbContext db, CancellationToken ct)
    {
        var context = await DispatcherContext(user, db, ct); if (context is null) return Results.Forbid();
        var vehicle = await db.Vehicles.SingleOrDefaultAsync(x => x.Id == vehicleId && x.ProviderId == context.ProviderId, ct); if (vehicle is null) return Results.NotFound();
        vehicle.IsActive = false; await db.SaveChangesAsync(ct); return Results.NoContent();
    }

    private static async Task<IResult> AssignJourney(string journeyKey, AssignTripRequest request, ClaimsPrincipal user, ApplicationDbContext db, CancellationToken ct)
    {
        var context = await DispatcherContext(user, db, ct); if (context is null) return Results.Forbid();
        var trips = await db.Trips.Where(x => x.ProviderId == context.ProviderId && x.JourneyKey == journeyKey && x.IsActive).ToListAsync(ct);
        return trips.Count == 0 ? Results.NotFound() : await Assign(trips, request, context, db, ct);
    }

    private static async Task<IResult> AssignTrip(string tripNumber, AssignTripRequest request, ClaimsPrincipal user, ApplicationDbContext db, CancellationToken ct)
    {
        var context = await DispatcherContext(user, db, ct); if (context is null) return Results.Forbid();
        var trip = await db.Trips.SingleOrDefaultAsync(x => x.ProviderId == context.ProviderId && x.TripNumber == tripNumber, ct);
        if (trip is null) return Results.NotFound();
        if (!trip.IsActive) return Results.BadRequest(new { message = "An inactive or broker-invalid Trip cannot be assigned." });
        return await Assign([trip], request, context, db, ct);
    }

    private static async Task<IResult> Assign(IReadOnlyList<Trip> trips, AssignTripRequest request, ProviderContext context, ApplicationDbContext db, CancellationToken ct)
    {
        var driver = await db.Drivers.SingleOrDefaultAsync(x => x.Id == request.DriverId && x.ProviderId == context.ProviderId && x.IsActive, ct);
        var vehicle = await db.Vehicles.SingleOrDefaultAsync(x => x.Id == request.VehicleId && x.ProviderId == context.ProviderId && x.IsActive, ct);
        if (driver is null || vehicle is null) return Results.BadRequest(new { message = "Choose active Driver and Vehicle records for this Provider." });
        var ids = trips.Select(x => x.Id).ToArray();
        var journeyKeys = trips.Select(x => x.JourneyKey).Distinct().ToArray();
        var otherJourneyDriverIds = await (from trip in db.Trips
                                           join assignment in db.TripAssignments on trip.Id equals assignment.TripId
                                           where trip.ProviderId == context.ProviderId && journeyKeys.Contains(trip.JourneyKey)
                                               && !ids.Contains(trip.Id) && assignment.SupersededAt == null
                                           select assignment.DriverId).Distinct().ToListAsync(ct);
        var active = await db.TripAssignments.Where(x => ids.Contains(x.TripId) && x.SupersededAt == null).ToListAsync(ct);
        foreach (var assignment in active) assignment.SupersededAt = DateTimeOffset.UtcNow;
        foreach (var trip in trips) db.TripAssignments.Add(new TripAssignment { TripId = trip.Id, DriverId = driver.Id, VehicleId = vehicle.Id, AssignedByAppUserId = context.AppUserId });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { message = "This Trip was assigned by another Dispatcher. Refresh and try again." });
        }
        return Results.Ok(new { assignedTripNumbers = trips.Select(x => x.TripNumber), warning = otherJourneyDriverIds.Any(id => id != driver.Id) });
    }

    private static async Task<IResult> AssignmentHistory(string tripNumber, ClaimsPrincipal user, ApplicationDbContext db, CancellationToken ct)
    {
        var context = await DispatcherContext(user, db, ct); if (context is null) return Results.Forbid();
        var trip = await db.Trips.SingleOrDefaultAsync(x => x.ProviderId == context.ProviderId && x.TripNumber == tripNumber, ct); if (trip is null) return Results.NotFound();
        return Results.Ok(await db.TripAssignments.Where(x => x.TripId == trip.Id).OrderBy(x => x.AssignedAt)
            .Select(x => new AssignmentResponse(x.DriverId, x.VehicleId, x.AssignedByAppUserId, x.AssignedAt, x.SupersededAt)).ToListAsync(ct));
    }

    private static async Task<IResult> DriverAssignments(ClaimsPrincipal user, ApplicationDbContext db, CancellationToken ct)
    {
        var context = await ProviderContextResolver.ResolveActive(user, db, ct);
        if (context is null || !ProviderContextResolver.HasRole(context, "Driver")) return Results.Forbid();
        var driverId = await db.Drivers.Where(x => x.ProviderId == context.ProviderId && x.AppUserId == context.AppUserId && x.IsActive).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        if (!driverId.HasValue) return Results.Forbid();
        return Results.Ok(await (from assignment in db.TripAssignments
                                 join trip in db.Trips on assignment.TripId equals trip.Id
                                 where assignment.DriverId == driverId && assignment.SupersededAt == null && trip.IsActive
                                 orderby trip.AppointmentDate, trip.AppointmentTime
                                 select new { trip.TripNumber, trip.JourneyKey, assignment.VehicleId }).ToListAsync(ct));
    }

    private static async Task<ProviderContext?> DispatcherContext(ClaimsPrincipal user, ApplicationDbContext db, CancellationToken ct)
    {
        var context = await ProviderContextResolver.ResolveActive(user, db, ct);
        return ProviderContextResolver.HasRole(context, "Dispatcher") ? context : null;
    }
}
