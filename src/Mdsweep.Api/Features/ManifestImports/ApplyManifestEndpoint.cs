using System.Security.Claims;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.ManifestImports;

namespace Mdsweep.Api.Features.ManifestImports;

public static class ApplyManifestEndpoint
{
    [WolverinePost("/api/manifest-imports/{previewId:guid}/apply")]
    public static async Task<IResult> Apply(
        Guid previewId,
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
            new ApplyManifest(context.TenantId, previewId),
            cancellationToken
        );

        return response.Found
            ? Results.Ok(new { response.Imported, response.Blocked })
            : Results.NotFound();
    }
}
