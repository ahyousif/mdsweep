using Mdsweep.Application.Dispatch;
using Mdsweep.Domain.Dispatch;
using Mdsweep.Domain.Identity;
using Mdsweep.Domain.ManifestImports;
using Mdsweep.Infrastructure.Identity;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Infrastructure.Dispatch;

public static class TripAssignmentHandler
{
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
