# Use NodaTime and UUIDv7

MDSweep will represent timeline instants, local service dates, and local wall-clock times with NodaTime rather than `DateTimeOffset`, `DateOnly`, and `TimeOnly`, because transportation planning must distinguish an absolute event from a Provider-local date or time and must not inherit the server's time zone. New entity and idempotent-action identifiers will use .NET's `Guid.CreateVersion7()` so PostgreSQL UUID keys remain globally unique while gaining time-ordered locality; the PostgreSQL column type remains `uuid`.
