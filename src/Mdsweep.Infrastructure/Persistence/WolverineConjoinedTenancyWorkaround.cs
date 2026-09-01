namespace Mdsweep.Infrastructure.Persistence;

public static class WolverineConjoinedTenancyWorkaround
{
    /// <summary>
    /// TEMPORARY WORKAROUND: Wolverine managed conjoined tenancy registers normal DI
    /// ApplicationDbContext resolution against the main/default tenant. In lightweight
    /// transaction mode, handlers depending on IRepository therefore need this scoped
    /// context factory to use the active IMessageBus tenant.
    ///
    /// Eager mode is currently unusable with WithDbContextAbstraction&lt;IRepository,
    /// ApplicationDbContext&gt;: Wolverine 6.30.3 generates duplicate concrete
    /// ApplicationDbContext variables (CS0841/CS0136). Remove this workaround when the
    /// upstream Wolverine defect is fixed.
    /// </summary>
    public static IServiceCollection AddWolverineConjoinedTenancyWorkaround(this IServiceCollection services)
    {
        services.AddScoped<ApplicationDbContext>(serviceProvider =>
            serviceProvider
                .GetRequiredService<IDbContextBuilder<ApplicationDbContext>>()
                .BuildAsync(
                    serviceProvider.GetRequiredService<IMessageBus>().TenantId ?? StorageConstants.DefaultTenantId,
                    CancellationToken.None
                )
                .AsTask()
                .GetAwaiter()
                .GetResult()
        );

        return services;
    }
}
