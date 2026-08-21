using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mdsweep.Api.Infrastructure;
using Mdsweep.Api.Features.Dispatch;

namespace Mdsweep.Api.Features.ManifestImports;

public static class ManifestImportEndpoints
{
    public static IEndpointRouteBuilder MapManifestImports(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/manifest-imports/preview", Preview)
            .RequireAuthorization(policy => policy.RequireRole("Dispatcher"))
            .DisableAntiforgery();
        endpoints.MapPost("/api/manifest-imports/{previewId:guid}/apply", Apply)
            .RequireAuthorization(policy => policy.RequireRole("Dispatcher"));
        endpoints.MapGet("/api/manifest-imports/{previewId:guid}", GetPreview)
            .RequireAuthorization(policy => policy.RequireRole("Dispatcher"));
        endpoints.MapGet("/api/service-days/{serviceDate}/trips", GetServiceDay)
            .RequireAuthorization(policy => policy.RequireRole("Dispatcher"));
        return endpoints;
    }

    private static async Task<IResult> Preview(
        IFormFile? file,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { message = "Choose a non-empty MTM CSV file." });
        if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { message = "Upload an MTM CSV file. Other spreadsheet formats are not supported yet." });

        try
        {
            await using var stream = file.OpenReadStream();
            var parsedRows = await ManifestCsv.Preview(stream, cancellationToken);
            var rows = await IdentifyBrokerChanges(parsedRows, db, cancellationToken);
            var preview = new ManifestPreview
            {
                FileName = Path.GetFileName(file.FileName),
                RowsJson = JsonSerializer.Serialize(rows)
            };
            db.ManifestPreviews.Add(preview);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new ManifestPreviewResponse(
                preview.Id,
                rows.Count(x => x.Disposition == ManifestRowDisposition.Ready),
                rows.Count(x => x.Disposition == ManifestRowDisposition.Warning),
                rows.Count(x => x.Disposition == ManifestRowDisposition.Blocked),
                rows.Where(x => x.AppointmentDate.HasValue).Select(x => x.AppointmentDate!.Value).Distinct().Order().ToArray(),
                rows));
        }
        catch (ManifestFormatException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }

    private static async Task<IResult> Apply(
        Guid previewId,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var preview = await db.ManifestPreviews.FindAsync([previewId], cancellationToken);
        if (preview is null) return Results.NotFound();
        var rows = JsonSerializer.Deserialize<List<ManifestPreviewRow>>(preview.RowsJson) ?? [];
        var importable = rows.Where(x => x.Disposition.IsImportable()).ToArray();
        if (preview.AppliedAt.HasValue)
            return Results.Ok(new { Imported = importable.Length, Blocked = rows.Count - importable.Length });

        var tripNumbers = importable.Select(row => row.TripNumber).ToArray();
        var existing = await db.Trips.Where(x => tripNumbers.Contains(x.TripNumber))
            .ToDictionaryAsync(x => x.TripNumber, StringComparer.OrdinalIgnoreCase, cancellationToken);
        foreach (var row in importable)
        {
            if (!existing.TryGetValue(row.TripNumber, out var trip))
            {
                trip = new Trip
                {
                    TripNumber = row.TripNumber,
                    JourneyKey = JourneyKey(row.TripNumber)
                };
                db.Trips.Add(trip);
                existing.Add(row.TripNumber, trip);
            }
            trip.ReconcileBrokerFields(row);
            db.TripBrokerImports.Add(new TripBrokerImport
            {
                TripId = trip.Id,
                ManifestPreviewId = preview.Id,
                TripNumber = row.TripNumber,
                AppointmentDate = row.AppointmentDate!.Value,
                AppointmentTime = row.AppointmentTime!.Value,
                PickupAddress = row.PickupAddress,
                DeliveryAddress = row.DeliveryAddress,
                BrokerStatus = row.BrokerStatus
            });
        }
        preview.AppliedAt ??= DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { Imported = importable.Length, Blocked = rows.Count - importable.Length });
    }

    private static async Task<IResult> GetPreview(
        Guid previewId,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var preview = await db.ManifestPreviews.AsNoTracking().SingleOrDefaultAsync(x => x.Id == previewId, cancellationToken);
        if (preview is null) return Results.NotFound();
        var rows = JsonSerializer.Deserialize<List<ManifestPreviewRow>>(preview.RowsJson) ?? [];
        return Results.Ok(new ManifestPreviewResponse(
            preview.Id,
            rows.Count(x => x.Disposition == ManifestRowDisposition.Ready),
            rows.Count(x => x.Disposition == ManifestRowDisposition.Warning),
            rows.Count(x => x.Disposition == ManifestRowDisposition.Blocked),
            rows.Where(x => x.AppointmentDate.HasValue).Select(x => x.AppointmentDate!.Value).Distinct().Order().ToArray(),
            rows));
    }

    private static async Task<IResult> GetServiceDay(
        DateOnly serviceDate,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var trips = await db.Trips.Where(x => x.AppointmentDate == serviceDate)
            .OrderBy(x => x.AppointmentTime)
            .Select(x => new ServiceDayTripResponse(
                x.TripNumber, x.JourneyKey, x.MemberFirstName + " " + x.MemberLastName,
                x.PickupAddress, x.PickupCity, x.DeliveryAddress, x.DeliveryCity,
                x.PassengerType, x.VehicleType, x.BrokerStatus, x.AppointmentTime,
                db.TripSchedules.Where(schedule => schedule.TripId == x.Id)
                    .Select(schedule => (TimeOnly?)schedule.ScheduledPickupTime).SingleOrDefault(),
                x.IsWillCall, x.IsActive))
            .ToListAsync(cancellationToken);
        return Results.Ok(trips);
    }

    private static string JourneyKey(string tripNumber) =>
        tripNumber.Length > 1 && (tripNumber.EndsWith('A') || tripNumber.EndsWith('B'))
            ? tripNumber[..^1]
            : tripNumber;

    private static async Task<IReadOnlyList<ManifestPreviewRow>> IdentifyBrokerChanges(
        IReadOnlyList<ManifestPreviewRow> rows,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var tripNumbers = rows.Where(row => row.Disposition.IsImportable())
            .Select(row => row.TripNumber)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var existing = await db.Trips.AsNoTracking()
            .Where(trip => tripNumbers.Contains(trip.TripNumber))
            .ToDictionaryAsync(trip => trip.TripNumber, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var scheduledTripIds = await db.TripSchedules.AsNoTracking()
            .Where(schedule => existing.Values.Select(trip => trip.Id).Contains(schedule.TripId))
            .Select(schedule => schedule.TripId)
            .ToHashSetAsync(cancellationToken);

        return rows.Select(row =>
        {
            if (!row.Disposition.IsImportable())
                return row with { BrokerChange = ManifestBrokerChange.Blocked };
            if (!existing.TryGetValue(row.TripNumber, out var trip))
                return row with { BrokerChange = ManifestBrokerChange.New };
            var differences = trip.BrokerDifferences(row);
            return differences.Count == 0
                ? row with { BrokerChange = ManifestBrokerChange.Unchanged }
                : row with
                {
                    BrokerChange = ManifestBrokerChange.BrokerChanged,
                    Messages = row.Messages
                        .Append($"MTM changed: {string.Join(", ", differences)}.")
                        .Concat(scheduledTripIds.Contains(trip.Id)
                            ? ["Your scheduled pickup time will be preserved."]
                            : [])
                        .ToArray()
                };
        }).ToArray();
    }
}
