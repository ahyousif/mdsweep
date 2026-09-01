using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Common.Extensions;

public static class MessageBusExtensions
{
    public static Task<Result<T>> SendAsync<T>(
        this IMessageBus bus,
        IRequest<T> request,
        CancellationToken ct = default
    ) => bus.InvokeAsync<Result<T>>(request, ct);

    public static Task<PagedResult<T>> SendAsync<T>(
        this IMessageBus bus,
        IRequest<PagedResult<T>> request,
        CancellationToken ct = default
    ) => bus.InvokeAsync<PagedResult<T>>(request, ct);
}
