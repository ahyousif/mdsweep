using Mdsweep.Application.Dispatch;
using Mdsweep.Domain.Dispatch;
using Mdsweep.Domain.Identity;
using Mdsweep.Domain.ManifestImports;
using Mdsweep.Infrastructure.Identity;
using Mdsweep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;

namespace Mdsweep.Infrastructure.Dispatch;

public static class DispatchManagementHandler
{
    public static Task<List<DriverResponse>> Handle(
        ListDrivers query,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    ) =>
        db
            .Drivers.Where(x => x.ProviderId == query.ProviderId)
            .OrderBy(x => x.DisplayName)
            .Select(x => new DriverResponse(
                x.Id,
                x.AppUserId,
                x.DisplayName,
                x.MtmDriverNumber,
                x.IsActive
            ))
            .ToListAsync(cancellationToken);

    [Transactional]
    public static async Task<DispatchManagementResult<DriverResponse>> Handle(
        CreateDriver command,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    )
    {
        var request = command.Request;
        if (
            string.IsNullOrWhiteSpace(request.DisplayName)
            || string.IsNullOrWhiteSpace(request.MtmDriverNumber)
        )
        {
            return Invalid<DriverResponse>("A Driver name and MTM Driver Number are required.");
        }

        var hasMembership = await db.ProviderMemberships.AnyAsync(
            x =>
                x.ProviderId == command.ProviderId
                && x.AppUserId == request.AppUserId
                && x.Role == "Driver",
            cancellationToken
        );
        if (!hasMembership)
        {
            return Invalid<DriverResponse>("The App User must be a Driver for this Provider.");
        }

        var driver = new Driver
        {
            ProviderId = command.ProviderId,
            AppUserId = request.AppUserId,
            DisplayName = request.DisplayName.Trim(),
            MtmDriverNumber = request.MtmDriverNumber.Trim(),
        };
        db.Drivers.Add(driver);

        return Created(
            $"/api/drivers/{driver.Id}",
            new DriverResponse(
                driver.Id,
                driver.AppUserId,
                driver.DisplayName,
                driver.MtmDriverNumber,
                driver.IsActive
            )
        );
    }

    public static async Task<DispatchManagementResult<DriverResponse>> Handle(
        CreateDriverAccess command,
        ApplicationDbContext db,
        IKeycloakUserAdministration keycloak,
        CancellationToken cancellationToken
    )
    {
        var request = command.Request;
        if (
            string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.TemporaryPassword)
            || string.IsNullOrWhiteSpace(request.DisplayName)
            || string.IsNullOrWhiteSpace(request.MtmDriverNumber)
        )
        {
            return Invalid<DriverResponse>(
                "Email, temporary password, Driver name, and MTM Driver Number are required."
            );
        }

        var organizationId = await db
            .Providers.Where(x => x.Id == command.ProviderId)
            .Select(x => x.KeycloakOrganizationId)
            .SingleAsync(cancellationToken);
        var subject = await keycloak.CreateDriverAsync(
            request.Email.Trim(),
            request.TemporaryPassword,
            organizationId,
            cancellationToken
        );
        var appUser = new AppUser { KeycloakSubject = subject };
        var driver = new Driver
        {
            ProviderId = command.ProviderId,
            AppUserId = appUser.Id,
            DisplayName = request.DisplayName.Trim(),
            MtmDriverNumber = request.MtmDriverNumber.Trim(),
        };
        db.AppUsers.Add(appUser);
        db.ProviderMemberships.Add(
            new ProviderMembership
            {
                ProviderId = command.ProviderId,
                AppUserId = appUser.Id,
                Role = "Driver",
            }
        );
        db.Drivers.Add(driver);

        // Keep the existing compensation workflow: Keycloak must be cleaned up
        // if the local identity and Driver records cannot be committed.
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await keycloak.DeleteUserAsync(subject, CancellationToken.None);
            throw;
        }

        return Created(
            $"/api/drivers/{driver.Id}",
            new DriverResponse(
                driver.Id,
                appUser.Id,
                driver.DisplayName,
                driver.MtmDriverNumber,
                driver.IsActive
            )
        );
    }

    public static async Task<DispatchManagementResult<bool>> Handle(
        ResetDriverAccess command,
        ApplicationDbContext db,
        IKeycloakUserAdministration keycloak,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(command.Request.TemporaryPassword))
        {
            return Invalid<bool>("A temporary password is required.");
        }

        var driver = await db.Drivers.SingleOrDefaultAsync(
            x => x.Id == command.DriverId && x.ProviderId == command.ProviderId,
            cancellationToken
        );
        if (driver is null)
            return NotFound<bool>();

        var subject = await db
            .AppUsers.Where(x => x.Id == driver.AppUserId)
            .Select(x => x.KeycloakSubject)
            .SingleAsync(cancellationToken);
        await keycloak.ResetPasswordAsync(
            subject,
            command.Request.TemporaryPassword,
            cancellationToken
        );
        return Success(true);
    }

    [Transactional]
    public static async Task<DispatchManagementResult<bool>> Handle(
        DeactivateDriver command,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    )
    {
        var driver = await db.Drivers.SingleOrDefaultAsync(
            x => x.Id == command.DriverId && x.ProviderId == command.ProviderId,
            cancellationToken
        );
        if (driver is null)
            return NotFound<bool>();
        driver.IsActive = false;
        return Success(true);
    }

    public static Task<List<VehicleResponse>> Handle(
        ListVehicles query,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    ) =>
        db
            .Vehicles.Where(x => x.ProviderId == query.ProviderId)
            .OrderBy(x => x.DisplayName)
            .Select(x => new VehicleResponse(x.Id, x.DisplayName, x.Vin, x.IsActive))
            .ToListAsync(cancellationToken);

    [Transactional]
    public static Task<DispatchManagementResult<VehicleResponse>> Handle(
        CreateVehicle command,
        ApplicationDbContext db
    )
    {
        var request = command.Request;
        if (
            string.IsNullOrWhiteSpace(request.DisplayName) || string.IsNullOrWhiteSpace(request.Vin)
        )
        {
            return Task.FromResult(
                Invalid<VehicleResponse>("A Vehicle name and VIN are required.")
            );
        }

        var vehicle = new Vehicle
        {
            ProviderId = command.ProviderId,
            DisplayName = request.DisplayName.Trim(),
            Vin = request.Vin.Trim(),
        };
        db.Vehicles.Add(vehicle);
        return Task.FromResult(
            Created(
                $"/api/vehicles/{vehicle.Id}",
                new VehicleResponse(vehicle.Id, vehicle.DisplayName, vehicle.Vin, vehicle.IsActive)
            )
        );
    }

    [Transactional]
    public static async Task<DispatchManagementResult<bool>> Handle(
        DeactivateVehicle command,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    )
    {
        var vehicle = await db.Vehicles.SingleOrDefaultAsync(
            x => x.Id == command.VehicleId && x.ProviderId == command.ProviderId,
            cancellationToken
        );
        if (vehicle is null)
            return NotFound<bool>();
        vehicle.IsActive = false;
        return Success(true);
    }

    public static async Task<DispatchManagementResult<AssignmentMutationResponse>> Handle(
        AssignJourney command,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    )
    {
        var trips = await db
            .Trips.Where(x =>
                x.ProviderId == command.ProviderId
                && x.JourneyKey == command.JourneyKey
                && x.IsActive
            )
            .ToListAsync(cancellationToken);
        return trips.Count == 0
            ? NotFound<AssignmentMutationResponse>()
            : await Assign(
                trips,
                command.Request,
                command.ProviderId,
                command.AppUserId,
                db,
                cancellationToken
            );
    }

    public static async Task<DispatchManagementResult<AssignmentMutationResponse>> Handle(
        AssignSingleTrip command,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    )
    {
        var trip = await db.Trips.SingleOrDefaultAsync(
            x => x.ProviderId == command.ProviderId && x.TripNumber == command.TripNumber,
            cancellationToken
        );
        if (trip is null)
            return NotFound<AssignmentMutationResponse>();
        if (!trip.IsActive)
        {
            return Invalid<AssignmentMutationResponse>(
                "An inactive or broker-invalid Trip cannot be assigned."
            );
        }

        return await Assign(
            [trip],
            command.Request,
            command.ProviderId,
            command.AppUserId,
            db,
            cancellationToken
        );
    }

    public static async Task<DispatchManagementResult<IReadOnlyList<AssignmentResponse>>> Handle(
        GetAssignmentHistory query,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    )
    {
        var trip = await db.Trips.SingleOrDefaultAsync(
            x => x.ProviderId == query.ProviderId && x.TripNumber == query.TripNumber,
            cancellationToken
        );
        if (trip is null)
            return NotFound<IReadOnlyList<AssignmentResponse>>();

        var history = await db
            .TripAssignments.Where(x => x.TripId == trip.Id)
            .OrderBy(x => x.AssignedAt)
            .Select(x => new AssignmentResponse(
                x.DriverId,
                x.VehicleId,
                x.AssignedByAppUserId,
                x.AssignedAt,
                x.SupersededAt
            ))
            .ToListAsync(cancellationToken);
        return Success<IReadOnlyList<AssignmentResponse>>(history);
    }

    private static async Task<DispatchManagementResult<AssignmentMutationResponse>> Assign(
        IReadOnlyList<Trip> trips,
        AssignTripRequest request,
        Guid providerId,
        Guid appUserId,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    )
    {
        var driver = await db.Drivers.SingleOrDefaultAsync(
            x => x.Id == request.DriverId && x.ProviderId == providerId && x.IsActive,
            cancellationToken
        );
        var vehicle = await db.Vehicles.SingleOrDefaultAsync(
            x => x.Id == request.VehicleId && x.ProviderId == providerId && x.IsActive,
            cancellationToken
        );
        if (driver is null || vehicle is null)
        {
            return Invalid<AssignmentMutationResponse>(
                "Choose active Driver and Vehicle records for this Provider."
            );
        }

        var ids = trips.Select(x => x.Id).ToArray();
        var journeyKeys = trips.Select(x => x.JourneyKey).Distinct().ToArray();
        var otherJourneyDriverIds = await (
            from trip in db.Trips
            join assignment in db.TripAssignments on trip.Id equals assignment.TripId
            where
                trip.ProviderId == providerId
                && journeyKeys.Contains(trip.JourneyKey)
                && !ids.Contains(trip.Id)
                && assignment.SupersededAt == null
            select assignment.DriverId
        )
            .Distinct()
            .ToListAsync(cancellationToken);
        var active = await db
            .TripAssignments.Where(x => ids.Contains(x.TripId) && x.SupersededAt == null)
            .ToListAsync(cancellationToken);
        foreach (var assignment in active)
        {
            assignment.SupersededAt = DateTimeOffset.UtcNow;
        }

        foreach (var trip in trips)
        {
            db.TripAssignments.Add(
                new TripAssignment
                {
                    TripId = trip.Id,
                    DriverId = driver.Id,
                    VehicleId = vehicle.Id,
                    AssignedByAppUserId = appUserId,
                }
            );
        }

        // Preserve the existing optimistic-concurrency response contract.
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return new DispatchManagementResult<AssignmentMutationResponse>(
                DispatchManagementOutcome.Conflict,
                Message: "This Trip was assigned by another Dispatcher. Refresh and try again."
            );
        }

        return Success(
            new AssignmentMutationResponse(
                trips.Select(x => x.TripNumber).ToArray(),
                otherJourneyDriverIds.Any(id => id != driver.Id)
            )
        );
    }

    private static DispatchManagementResult<T> Success<T>(T value) =>
        new(DispatchManagementOutcome.Success, value);

    private static DispatchManagementResult<T> Created<T>(string location, T value) =>
        new(DispatchManagementOutcome.Success, value, Location: location);

    private static DispatchManagementResult<T> Invalid<T>(string message) =>
        new(DispatchManagementOutcome.BadRequest, Message: message);

    private static DispatchManagementResult<T> NotFound<T>() =>
        new(DispatchManagementOutcome.NotFound);
}
