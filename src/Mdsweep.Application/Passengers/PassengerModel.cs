namespace Mdsweep.Application.Passengers;

public sealed record PassengerModel
{
    public Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
}
