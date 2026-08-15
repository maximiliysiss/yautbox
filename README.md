# Yautbox (Yet Another Outbox)

[![.NET](https://github.com/maximiliysiss/yautbox/actions/workflows/dotnet.yml/badge.svg?branch=master&event=push)](https://github.com/maximiliysiss/yautbox/actions/workflows/dotnet.yml)
[![NuGet](https://img.shields.io/nuget/v/Yautbox)](https://www.nuget.org/packages/Yautbox/)

[Release notes](releasenotes/README.md)

Yautbox is a lightweight .NET outbox library. It lets you enqueue messages during application work and process them
later with background handlers. The core package is storage-agnostic; choose the in-memory or PostgreSQL provider, or
implement your own storage provider.

## Features

- Outbox pattern with background processing
- Typed handlers with retry support
- Scheduled messages and visibility timeouts
- Configurable concurrency, polling, and cleanup
- Pluggable storage via `IOutboxProvider`

## Delivery model

- Delivery is **at least once**: a message can be delivered more than once when a handler succeeds but persisting its result fails.
- Processing order is not guaranteed, especially with multiple workers or parallel batches.
- Handlers must be idempotent.

## Installation

NuGet:

```bash
dotnet add package Yautbox
dotnet add package Yautbox.InMemory   # optional provider
dotnet add package Yautbox.Postgres   # optional provider
```

## Quick start

1) Register the outbox and a provider.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Yautbox.Extensions.Ioc;
using Yautbox.InMemory.Extensions; // from Yautbox.InMemory package

services.AddOutbox(builder => builder.UseInMemory());
```

2) Register a handler for a payload type.

```csharp
using Yautbox.Extensions.Ioc;
using Yautbox.Handlers;

services.AddOutboxHandler<OrderPlaced, OrderPlacedHandler>();

public sealed class OrderPlacedHandler : IOutboxHandler<OrderPlaced>
{
    public Task HandleAsync(IEnumerable<OutboxMessage<OrderPlaced>> messages, CancellationToken ct)
    {
        foreach (var message in messages)
        {
            // process message.Payload
            // message.Retry(TimeSpan.FromMinutes(1)); // optional retry
        }

        return Task.CompletedTask;
    }
}
```

3) Enqueue messages from your app code.

```csharp
using Yautbox.Services;

await outbox.HandleAsync(new[] { new OrderPlaced("A-123") });
await outbox.HandleAsync(
    new[] { new OrderPlaced("B-456") },
    scheduledAt: DateTimeOffset.UtcNow.AddMinutes(5));
```

Cancel by id if needed:

```csharp
using Yautbox.Extensions.Outbox;

await outbox.CancelAsync<OrderPlaced>(messageId);
```

For a heterogeneous batch, resolve `IPolymorphicOutboxService`. Each payload is stored under its concrete runtime type
and routed to that type's handler; returned identifiers preserve the input order:

```csharp
IPolymorphicOutboxService polymorphicOutbox = /* resolve from DI */;
object[] events = [new OrderPlaced("A-123"), new CustomerRegistered("C-456")];

IEnumerable<OutboxMessageId> ids = await polymorphicOutbox.HandleAsync(events);
```

## Configuration options

Each handler can be configured via `AddOutboxHandler().ConfigureOptions<TOptions>()`. Implement
`IOutboxRunnerOptions` for full control or `ISimpleRunnerOptions` when only `BufferSize` must be supplied.

```csharp
using Microsoft.Extensions.Options;
using Yautbox.Extensions.Ioc;
using Yautbox.Runner.Options;

services
    .AddOutboxHandler<OrderPlaced, OrderPlacedHandler>()
    .ConfigureOptions<DefaultRunnerOptions>(options =>
        options.Configure(o =>
        {
            o.BufferSize = 500;
            o.WorkersCount = 2;
            o.ExecutionPolicy = ExecutionPolicy.Parallel;
            o.BackupInterval = TimeSpan.FromHours(24);
        }));
```

`IOutboxRunnerOptions` settings and defaults:

- `Identifier` (default: payload type assembly-qualified name without version, culture, or public key token)
- `PollDelay` (default: 5s + jitter)
- `BufferSize` (default: 1000)
- `PerBufferCount` (default: BufferSize)
- `HandleTimeout` (default: 55m)
- `IsEnabled` (default: true)
- `WorkersCount` (default: 1)
- `DeletePolicy` (default: Safe)
- `FailureDelay` (default: 2s + jitter)
- `Visibility` (default: 1h)
- `BackupInterval` (default: null, disabled)
- `CleanupInterval` (default: 1d)
- `ExecutionPolicy` (default: Parallel)
- `CancellationPolicy` (default: Safe)
- `PolicyTimeout` (default: 55m)
- `ScopeLifetime` (default: PerBatch)

### Infrastructure builder options

`AddOutbox(Action<IOutboxInfrastructureBuilder>)` accepts additional infrastructure-level configuration:

- `SetPrefix(string prefix)` prepends a custom prefix to registry identifiers returned by the outbox registry.
- `SetRegistryPolicy(OutboxRegistryPolicy policy)` controls how the registry behaves for unregistered types:
  `Lenient` uses defaults and does not throw; `Strict` throws `RegistryStrictException`.
- `SetMetrics<T>()` replaces the default no-op `IMetricsHandler` to capture outbox lifecycle metrics.

Example:

```csharp
using Yautbox.Extensions.Ioc;
using Yautbox.InMemory.Extensions;
using Yautbox.Registy;

services.AddOutbox(builder =>
{
    builder.UseInMemory();
    builder.SetPrefix("myapp_");
    builder.SetRegistryPolicy(OutboxRegistryPolicy.Strict);
});
```

## Metrics

Yautbox reports lifecycle metrics through `IMetricsHandler`. The default handler is a no-op. Register a custom handler via the infrastructure builder:

```csharp
using Yautbox.Extensions.Ioc;
using Yautbox.Metrics;

services.AddOutbox(builder =>
{
    builder.UseInMemory();
    builder.SetMetrics<MyMetricsHandler>();
});

public sealed class MyMetricsHandler : IMetricsHandler
{
    public ValueTask AddedAsync(string identifier, int count, CancellationToken ct) => ValueTask.CompletedTask;
    public ValueTask CanceledAsync(string identifier, int count, CancellationToken ct) => ValueTask.CompletedTask;
    public ValueTask HandledAsync(string identifier, int count, TimeSpan elapsed, CancellationToken ct) => ValueTask.CompletedTask;
    public ValueTask RetriedAsync(string identifier, int count, CancellationToken ct) => ValueTask.CompletedTask;
    public ValueTask DeletedAsync(string identifier, int count, CancellationToken ct) => ValueTask.CompletedTask;
    public ValueTask CleanedInAsync(string identifier, TimeSpan elapsed, CancellationToken ct) => ValueTask.CompletedTask;
    public ValueTask ReadInAsync(string identifier, TimeSpan elapsed, CancellationToken ct) => ValueTask.CompletedTask;
    public ValueTask ErrorsAsync(string identifier, int count, CancellationToken ct) => ValueTask.CompletedTask;
}
```

## Tracing

Yautbox creates tracing scopes for enqueue, fetch, handle, persist, and cleanup operations through `IOutboxTracer`.
The default tracer is a no-op. Register an adapter for your tracing system with `SetTracing<T>()`:

```csharp
services.AddOutbox(builder =>
{
    builder.UsePostgres(connectionString);
    builder.SetTracing<MyOutboxTracer>();
});
```

An `IOutboxTraceScope` receives operation tags and failures and can be backed by `System.Diagnostics.Activity`,
OpenTelemetry, or another distributed tracing implementation.

## How it works

- `IOutboxService` enqueues messages into an `IOutboxProvider`.
- `AddOutboxHandler<TPayload, THandler>()` registers two hosted services:
    - a handler runner that polls, locks, and dispatches messages
    - a cleanup runner that deletes old handled records when `BackupInterval` is set
- `ExecutionPolicy.Sequential` uses a provider-level policy scope to ensure single active processing for the same
  identifier when the provider supports distributed locking.

## Providers

| Provider | Package            | Documentation                            | Durability          |
|----------|--------------------|------------------------------------------|---------------------|
| InMemory | `Yautbox.InMemory` | [README](src/Yautbox.InMemory/README.md) | Process memory only |
| Postgres | `Yautbox.Postgres` | [README](src/Yautbox.Postgres/README.md) | PostgreSQL storage  |

## Target Framework

`net8.0`
