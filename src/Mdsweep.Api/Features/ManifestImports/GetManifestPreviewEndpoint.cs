using System.Security.Claims;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.ManifestImports;
using Wolverine;
using Wolverine.Http;

namespace Mdsweep.Api.Features.ManifestImports;

public static class GetManifestPreviewEndpoint
{
    [WolverineGet("/api/manifest-imports/{previewId:guid}")]
    public static async Task<IResult> Get(
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

        var response = await bus.InvokeAsync<GetManifestPreviewResult>(
            new GetManifestPreview(context.ProviderId, previewId),
            cancellationToken
        );

        return response.Found ? Results.Ok(response.Preview) : Results.NotFound();
    }
}
