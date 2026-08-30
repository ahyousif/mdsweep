using System.Security.Claims;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.ManifestImports;
using Wolverine;
using Wolverine.Http;

namespace Mdsweep.Api.Features.ManifestImports;

public static class ApplyManifestEndpoint
{
    [WolverinePost("/api/manifest-imports/{previewId:guid}/apply")]
    public static async Task<IResult> Apply(
        Guid previewId,
        ClaimsPrincipal user,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await ProviderContextResolver.ResolveActive(user, bus, cancellationToken);
        if (context is null || !ProviderContextResolver.HasRole(context, "Dispatcher"))
        {
            return Results.Forbid();
        }

        var response = await bus.InvokeAsync<ApplyManifestResult>(
            new ApplyManifest(context.ProviderId, previewId),
            cancellationToken
        );

        return response.Found
            ? Results.Ok(new { response.Imported, response.Blocked })
            : Results.NotFound();
    }
}
