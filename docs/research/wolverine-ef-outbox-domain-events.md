# Wolverine 6.35 EF outbox and aggregate domain events

Date: 2026-09-11  
Scope: Wolverine 6.35.0, MDSweep managed conjoined tenancy, EF Core Lightweight transactions  
Conclusion: `IDbContextOutbox<ApplicationDbContext>` is transactionally capable, but it is not a centralized drop-in replacement for handler-returned `OutgoingMessages` in MDSweep's current handler pipeline.

## Executive conclusion

The scoped generic outbox can scrape domain events from the `DbContext.ChangeTracker` and persist entity changes plus **durably routed** envelopes in the same EF `SaveChangesAsync()` transaction. This works with Lightweight semantics because EF supplies the transaction for that save. However, Wolverine only performs that work when application code explicitly calls `SaveChangesAndFlushMessagesAsync()`. Wolverine documents the API primarily for code outside Wolverine handlers, and its example makes the caller own both the context and the save/flush boundary. It therefore replaces per-handler outgoing-message plumbing with per-operation outbox/save plumbing; it does not centralize the existing `IRepository` handler model. [Official outbox documentation](https://github.com/JasperFx/wolverine/blob/c0b0ce61ca9ba886ff89666034f9b65715c22048/docs/guide/durability/efcore/outbox-and-inbox.md#L57-L103) [6.35.0 implementation](https://github.com/JasperFx/wolverine/blob/c0b0ce61ca9ba886ff89666034f9b65715c22048/src/Persistence/Wolverine.EntityFrameworkCore/DbContextOutbox.cs#L7-L35)

The supported centralized mechanism is still `PublishDomainEventsFromEntityFrameworkCore(...)` plus Wolverine's EF transactional middleware. Wolverine 6.35.0 contains a dedicated managed-multi-tenancy regression test and commit frame intended to scrape domain events before commit. MDSweep's observed failure therefore indicates that its `IRepository` abstraction/scoped-context workaround is not taking that managed-context commit path; substituting `IDbContextOutbox<T>` would work around the path rather than repair central middleware behavior. [Official domain-event guidance](https://github.com/JasperFx/wolverine/blob/c0b0ce61ca9ba886ff89666034f9b65715c22048/docs/guide/durability/efcore/domain-events.md#L89-L122) [6.35.0 managed-tenancy regression test](https://github.com/JasperFx/wolverine/blob/c0b0ce61ca9ba886ff89666034f9b65715c22048/src/Persistence/EfCoreTests.MultiTenancy/domain_events_scraping_with_managed_multi_tenancy.cs#L20-L81)

## What `IDbContextOutbox<T>` actually guarantees

`DbContextOutbox<T>.SaveChangesAndFlushMessagesAsync()`:

1. runs every registered `IDomainEventScraper` against its `DbContext`;
2. calls that context's `SaveChangesAsync()`;
3. commits an explicit current transaction if one exists; and
4. flushes captured outgoing messages only after the database work succeeds.

The registered aggregate scraper enumerates tracked entities, extracts configured events, and publishes each through the outbox's `MessageContext`. [Outbox source](https://github.com/JasperFx/wolverine/blob/c0b0ce61ca9ba886ff89666034f9b65715c22048/src/Persistence/Wolverine.EntityFrameworkCore/DbContextOutbox.cs#L7-L35) [Scraper source](https://github.com/JasperFx/wolverine/blob/c0b0ce61ca9ba886ff89666034f9b65715c22048/src/Persistence/Wolverine.EntityFrameworkCore/OutgoingDomainEvents.cs#L26-L61)

For a Wolverine-enabled context, durable outgoing/incoming envelopes are added to the same EF change tracker as application entities. In Lightweight mode, Wolverine deliberately relies on the implicit transaction of `SaveChangesAsync()` rather than opening an explicit transaction. This is the atomic path: aggregate changes and envelope rows participate in the same save. [EF envelope transaction source](https://github.com/JasperFx/wolverine/blob/c0b0ce61ca9ba886ff89666034f9b65715c22048/src/Persistence/Wolverine.EntityFrameworkCore/Internals/EfCoreEnvelopeTransaction.cs#L50-L110)

That capability has two important limits for MDSweep:

- The caller must use the outbox's `DbContext`, or otherwise prove that `IRepository` and `outbox.DbContext` are the exact same scoped instance.
- The caller must explicitly call `SaveChangesAndFlushMessagesAsync()`. Keeping `AutoApplyTransactions()` while also saving through a separate outbox `MessageContext` would create competing persistence/message-context boundaries. Wolverine's docs say the ordinary EF transactional middleware is easiest inside Wolverine handlers and present `IDbContextOutbox<T>` as the escape hatch outside them. [Official outbox guidance](https://github.com/JasperFx/wolverine/blob/c0b0ce61ca9ba886ff89666034f9b65715c22048/docs/guide/durability/efcore/outbox-and-inbox.md#L57-L70)

Consequently, there is no 6.35 configuration switch that makes `IDbContextOutbox<ApplicationDbContext>` silently wrap all existing `IRepository` handlers. A centralized bridge would require custom middleware/decorator code that selects the tenant-pinned context, shares a single message context, suppresses the existing save boundary, and calls the outbox flush. That is a larger custom transaction mechanism, not a minimal supported replacement.

## Managed conjoined tenancy caveats

Wolverine documents conjoined tenancy as a single shared database where the transactional middleware/outbox should behave like a single-tenant application, with each context pinned to the active tenant. [Official conjoined-tenancy documentation](https://github.com/JasperFx/wolverine/blob/c0b0ce61ca9ba886ff89666034f9b65715c22048/docs/guide/durability/efcore/multi-tenancy.md#L291-L308)

The normal managed handler path supports aggregate scraping: `ConjoinedDbContextBuilder.BuildAndEnrollAsync()` pins the context to the handler's tenant and creates an `EfCoreEnvelopeTransaction` with the registered scrapers. Wolverine 6.35.0 also adds a managed-tenancy commit frame that scrapes events and performs another save for durable envelopes before committing. [Conjoined builder source](https://github.com/JasperFx/wolverine/blob/c0b0ce61ca9ba886ff89666034f9b65715c22048/src/Persistence/Wolverine.EntityFrameworkCore/Internals/ConjoinedDbContextBuilder.cs#L88-L100) [Commit-frame source](https://github.com/JasperFx/wolverine/blob/c0b0ce61ca9ba886ff89666034f9b65715c22048/src/Persistence/Wolverine.EntityFrameworkCore/Codegen/StartDatabaseTransactionForDbContext.cs#L89-L151)

The tenant-explicit `IDbContextOutboxFactory` is not a solution for aggregate scraping in 6.35.0. Although it correctly builds a tenant-pinned context, it constructs `DbContextOutbox<T>` with an empty scraper array (`[]`). It can transactionally publish messages explicitly passed to the outbox, but it will not discover MDSweep's queued `AggregateRoot.DomainEvents`. [Factory source](https://github.com/JasperFx/wolverine/blob/c0b0ce61ca9ba886ff89666034f9b65715c22048/src/Persistence/Wolverine.EntityFrameworkCore/IDbContextOutboxFactory.cs#L26-L49)

Direct scoped resolution of `IDbContextOutbox<ApplicationDbContext>` does receive registered scrapers, but Wolverine's managed conjoined registration registers ordinary scoped `ApplicationDbContext` for the main/default tenant. MDSweep's custom scoped registration currently changes that behavior. Relying on the direct outbox would therefore also rely on MDSweep's temporary workaround and needs a focused integration proof; it is not an independently supported tenant-selection mechanism. [Conjoined registration source](https://github.com/JasperFx/wolverine/blob/c0b0ce61ca9ba886ff89666034f9b65715c22048/src/Persistence/Wolverine.EntityFrameworkCore/WolverineEntityCoreExtensions.cs#L175-L211)

There is a second tenant-context gap: the directly resolved `DbContextOutbox<T>` constructs a new `MessageContext` but does not set its `TenantId`. The factory-created form explicitly sets `TenantId`, but, as noted above, passes no scrapers. In exact 6.35.0 there is therefore no built-in outbox construction path that simultaneously provides a tenant-aware context, the ambient message tenant on published envelopes, and registered aggregate scrapers. [Direct outbox constructor](https://github.com/JasperFx/wolverine/blob/c0b0ce61ca9ba886ff89666034f9b65715c22048/src/Persistence/Wolverine.EntityFrameworkCore/DbContextOutbox.cs#L7-L17) [Factory construction](https://github.com/JasperFx/wolverine/blob/c0b0ce61ca9ba886ff89666034f9b65715c22048/src/Persistence/Wolverine.EntityFrameworkCore/IDbContextOutboxFactory.cs#L36-L49)

## Durable local queues

MDSweep currently configures PostgreSQL message persistence, Lightweight EF transactions, automatic transaction application, and EF domain-event scraping. It does **not** configure `Policies.UseDurableLocalQueues()`, `DefaultLocalQueue.UseDurableInbox()`, or an equivalent per-queue durability policy.

`PersistMessagesWithPostgresql()` supplies message storage; it does not by itself change local queues from their default buffered in-memory mode. Wolverine states that local queues are buffered in memory by default and that marking them durable persists every message until successful processing, allowing recovery after process/node failure. [Official local-queue documentation](https://github.com/JasperFx/wolverine/blob/c0b0ce61ca9ba886ff89666034f9b65715c22048/docs/guide/messaging/transports/local.md#L175-L204) [Official durability documentation](https://github.com/JasperFx/wolverine/blob/c0b0ce61ca9ba886ff89666034f9b65715c22048/docs/guide/durability/index.md#L367-L397)

In source, `UseDurableLocalQueues()` applies `UseDurableInbox()` to all local queues. Wolverine's send path persists envelopes through the active transaction only when the destination is durable; otherwise they remain buffered for the post-save flush. [Policy source](https://github.com/JasperFx/wolverine/blob/c0b0ce61ca9ba886ff89666034f9b65715c22048/src/Wolverine/WolverineOptions.Policies.cs#L105-L108) [Message send path](https://github.com/JasperFx/wolverine/blob/c0b0ce61ca9ba886ff89666034f9b65715c22048/src/Wolverine/Runtime/MessageBus.cs#L438-L489)

Therefore:

- `UseDurableLocalQueues()` is **not required merely for an in-process local event handler to run** while the process remains healthy.
- It **is required** (or an equivalent durability setting on the particular local queue) for local domain-event delivery to survive a crash/restart and for the event envelope to participate in PostgreSQL inbox/outbox persistence.
- With durable local routing, Wolverine notes that local messages go directly to the inbox; external outgoing messages use the outbox. Both can share the EF transaction with aggregate persistence. [Official EF inbox/outbox documentation](https://github.com/JasperFx/wolverine/blob/c0b0ce61ca9ba886ff89666034f9b65715c22048/docs/guide/durability/efcore/outbox-and-inbox.md#L1-L20)

## Recommendation

Do not replace the temporary per-handler `OutgoingMessages` workaround with `IDbContextOutbox<ApplicationDbContext>` as a production design. It is transactionally useful when code explicitly owns the EF save boundary, but it does not preserve MDSweep's current convention that handlers inject `IRepository` and Wolverine middleware owns persistence.

The smallest supported target remains:

1. `PublishDomainEventsFromEntityFrameworkCore<IAggregateRoot, DomainEvent>(...)`;
2. the normal managed conjoined EF transaction chain using the tenant-pinned `ApplicationDbContext`; and
3. durable local queues for production-grade local event delivery.

Before changing production configuration, reduce the remaining MDSweep-specific chain mismatch to an upstream reproduction involving `WithDbContextAbstraction<IRepository, ApplicationDbContext>()`, managed conjoined tenancy, and Lightweight mode. Separately, report/verify the 6.35.0 `IDbContextOutboxFactory` empty-scraper behavior if tenant-explicit outbox use is considered.

No production code or tests were changed for this investigation.
