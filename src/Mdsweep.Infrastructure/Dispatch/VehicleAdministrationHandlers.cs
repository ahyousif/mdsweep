using Mdsweep.Application.Dispatch;
using Mdsweep.Domain.Dispatch;
using Mdsweep.Domain.ManifestImports;
using Mdsweep.Infrastructure.Identity;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Infrastructure.Dispatch;

public static class VehicleAdministrationHandler
{
    public static Task<List<VehicleResponse>> Handle(
        ListVehicles query,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    ) =>
        db
            .Vehicles.Where(x => x.TenantId == query.TenantId)
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
            TenantId = command.TenantId,
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
            x => x.Id == command.VehicleId && x.TenantId == command.TenantId,
            cancellationToken
        );
        if (vehicle is null)
            return NotFound<bool>();
        vehicle.IsActive = false;
        return Success(true);
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
