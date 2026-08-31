using Mdsweep.Application.Dispatch;
using Mdsweep.Domain.Dispatch;
using Mdsweep.Domain.ManifestImports;
using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;
using Mdsweep.Infrastructure.Identity;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Infrastructure.Dispatch;

public static class DriverAdministrationHandler
{
    public static Task<List<DriverResponse>> Handle(
        ListDrivers query,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    ) =>
        db
            .Drivers.Where(x => x.TenantId == query.TenantId)
            .OrderBy(x => x.DisplayName)
            .Select(x => new DriverResponse(
                x.Id,
                x.UserId,
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

        var hasMembership = await db.TenantMemberships.AnyAsync(
            x =>
                x.TenantId == command.TenantId
                && x.UserId == request.UserId
                && x.Role == "Driver",
            cancellationToken
        );
        if (!hasMembership)
        {
            return Invalid<DriverResponse>("The User must be a Driver for this Tenant.");
        }

        var driver = new Driver
        {
            TenantId = command.TenantId,
            UserId = request.UserId,
            DisplayName = request.DisplayName,
            MtmDriverNumber = request.MtmDriverNumber,
        };
        db.Drivers.Add(driver);

        return Created(
            $"/api/drivers/{driver.Id}",
            new DriverResponse(
                driver.Id,
                driver.UserId,
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
            .Tenants.Where(x => x.Id == command.TenantId)
            .Select(x => x.KeycloakOrganizationId)
            .SingleAsync(cancellationToken);
        var subject = await keycloak.CreateDriverAsync(
            request.Email,
            request.TemporaryPassword,
            organizationId,
            cancellationToken
        );
        var user = UserAggregate.Create(request.DisplayName, "Driver", subject);
        var driver = new Driver
        {
            TenantId = command.TenantId,
            UserId = user.Id,
            DisplayName = request.DisplayName,
            MtmDriverNumber = request.MtmDriverNumber,
        };
        db.Users.Add(user);
        db.TenantMemberships.Add(TenantMembership.Create(command.TenantId, user.Id, "Driver"));
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
                user.Id,
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
            x => x.Id == command.DriverId && x.TenantId == command.TenantId,
            cancellationToken
        );
        if (driver is null)
            return NotFound<bool>();

        var subject = await db
            .Users.Where(x => x.Id == driver.UserId)
            .Select(x => x.KeycloakUserId)
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
            x => x.Id == command.DriverId && x.TenantId == command.TenantId,
            cancellationToken
        );
        if (driver is null)
            return NotFound<bool>();
        driver.IsActive = false;
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
