namespace Mdsweep.Application.Passengers.List;

public sealed record ListPassengersResult(
    IReadOnlyList<PassengerModel> Items,
    long TotalCount,
    int Page,
    int PageSize,
    long TotalPages
);
