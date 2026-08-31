using System.Security.Claims;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.ManifestImports;
using Mdsweep.Infrastructure.ManifestImports;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Api.Features.ManifestImports;

public static class PreviewManifestEndpoint
{
    [WolverinePost("/api/manifest-imports/preview")]
    public static async Task<IResult> Preview(
        IFormFile? file,
        ClaimsPrincipal user,
        ApplicationDbContext db,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await ProviderContextResolver.ResolveActive(user, db, cancellationToken);
        if (context is null || !ProviderContextResolver.HasRole(context, "Dispatcher"))
        {
            return Results.Forbid();
        }

        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(
                new { message = "Choose a non-empty MTM CSV or Excel file." }
            );
        }

        var extension = Path.GetExtension(file.FileName);
        if (
            !extension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase)
        )
        {
            return Results.BadRequest(new { message = "Upload an MTM CSV or Excel (.xlsx) file." });
        }

        try
        {
            await using var input = file.OpenReadStream();
            using var content = new MemoryStream();
            await input.CopyToAsync(content, cancellationToken);

            var response = await bus.InvokeAsync<ManifestPreviewResponse>(
                new PreviewManifest(
                    context.ProviderId,
                    Path.GetFileName(file.FileName),
                    extension,
                    content.ToArray()
                ),
                cancellationToken
            );

            return Results.Ok(response);
        }
        catch (ManifestFormatException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }
}
