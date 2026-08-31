using System.Security.Claims;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.ManifestImports;

namespace Mdsweep.Api.Features.ManifestImports;

public static class ApplyManifestEndpoint
{
    [WolverinePost("/api/manifest-receipts/{receiptId:guid}/apply")]
    public static async Task<IResult> Apply(
        Guid receiptId,
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await TenantContextResolver.ResolveActive(user, tenantAccess, cancellationToken);
        if (context is null || !TenantContextResolver.HasRole(context, "Dispatcher"))
        {
            return Results.Forbid();
        }

        var response = await bus.InvokeAsync<ApplyManifestResult>(
            new ApplyManifest(receiptId),
            cancellationToken
        );

        return response.Found
            ? Results.Ok(new { response.Imported, response.Blocked })
            : Results.NotFound();
    }
}
