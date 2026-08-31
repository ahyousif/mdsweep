using System.Security.Claims;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.ManifestImports;

namespace Mdsweep.Api.Features.ManifestImports;

public static class GetManifestPreviewEndpoint
{
    [WolverineGet("/api/manifest-imports/{previewId:guid}")]
    public static async Task<IResult> Get(
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

        var response = await bus.InvokeAsync<GetManifestPreviewResult>(
            new GetManifestPreview(context.TenantId, previewId),
            cancellationToken
        );

        return response.Found ? Results.Ok(response.Preview) : Results.NotFound();
    }
}
