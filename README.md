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
`IOutboxRunnerOptions<TPayload>` for full control or `ISimpleRunnerOptions<TPayload>` when only `BufferSize` must be supplied.

```csharp
using Microsoft.Extensions.Options;
using Yautbox.Extensions.Ioc;
using Yautbox.Runner.Options;

public sealed class OrderPlacedOptions : ISimpleRunnerOptions<OrderPlaced>
{
    public int BufferSize { get; set; } = 500;
    public int WorkersCount { get; set; } = 2;
    public TimeSpan? BackupInterval { get; set; } = TimeSpan.FromHours(24);
}

services
    .AddOutboxHandler<OrderPlaced, OrderPlacedHandler>()
    .ConfigureOptions<OrderPlacedOptions>(options =>
        options.Configure(o => o.BufferSize = 1000));
```

Options are bound to the handler payload: `ConfigureOptions<TOptions>()` accepts only types implementing
`IOutboxRunnerOptions<TPayload>` for the payload of that handler. Without `ConfigureOptions` the defaults below are used.

`IOutboxRunnerOptions<TPayload>` settings and defaults:

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
- `PolicyTimeout` (default: 5s; if the scope is busy, the cycle is skipped until the next poll)
- `ScopeLifetime` (default: PerBatch)
- `PartitionKey` (default: null, not partitioned)

### Options validation

Options are validated when a runner starts and after every options reload. All problems are reported at once:

- **Failures** (for example, non-positive delays or sizes, `PerBufferCount > BufferSize`, `RetryCount <= 0`,
  `RetryDelays` with zero or negative values, `Sequential` with
  `PerBufferCount != BufferSize`) are logged at `Error` level and stop the runner. With the default
  `BackgroundServiceExceptionBehavior.StopHost` the application stops, so a misconfigured handler fails fast instead
  of silently doing nothing.
- **Warnings** are only logged: `Sequential` without `RetryCount` and `RetryDelays` (a failing message holds back the
  queue indefinitely), and `PartitionKey` with `ScopeLifetime.PerGroup` or `PerSession` (one handler instance is
  called concurrently).

A disabled handler (`IsEnabled = false`) is not validated.

### Partitioning

Set `PartitionKey` to partition the messages of a cycle. The messages fetched in one cycle are grouped by
the key before being split into `PerBufferCount` chunks, so one `HandleAsync` call never mixes different keys. Chunks
are handled concurrently. With `ExecutionPolicy.Sequential` this is the way to parallelize processing inside the
acquired scope when messages with different keys can be handled independently.

```csharp
public sealed class OrderPlacedOptions : ISimpleRunnerOptions<OrderPlaced>
{
    public int BufferSize => 500;
    ExecutionPolicy IOutboxRunnerOptions<OrderPlaced>.ExecutionPolicy => ExecutionPolicy.Sequential;
    Func<OrderPlaced, object?>? IOutboxRunnerOptions<OrderPlaced>.PartitionKey => message => message.OrderId;
}
```

Register it with `AddOutboxHandler<OrderPlaced, OrderPlacedHandler>().ConfigureOptions<OrderPlacedOptions>()`.

With `ScopeLifetime.PerGroup` or `PerSession` all chunks share one handler instance, which must then be thread-safe.

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

    // Optional: default implementations are no-ops.
    public ValueTask CycledAsync(string identifier, CancellationToken ct) => ValueTask.CompletedTask;
    public ValueTask CycledFailedAsync(string identifier, CancellationToken ct) => ValueTask.CompletedTask;
}
```

`CycledAsync` and `CycledFailedAsync` report every worker processing loop that ended normally or with an error.
They have default no-op implementations, so existing handlers keep compiling.

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
  identifier: an advisory lock for Postgres, an in-process lock for InMemory. Several workers (`WorkersCount > 1`) and
  instances may run; only the one holding the scope processes a cycle, the others skip it until the next poll.

## Providers

| Provider | Package            | Documentation                            | Durability          |
|----------|--------------------|------------------------------------------|---------------------|
| InMemory | `Yautbox.InMemory` | [README](src/Yautbox.InMemory/README.md) | Process memory only |
| Postgres | `Yautbox.Postgres` | [README](src/Yautbox.Postgres/README.md) | PostgreSQL storage  |

## Testing handlers

`Yautbox.Testing` ([README](src/Yautbox.Testing/README.md)) builds `OutboxMessage<T>` instances for unit tests of a
handler, without storage or a host:

```csharp
using Yautbox.Testing;

var message = OutboxMessage.Of(new OrderPlaced("A-123"), attempt: 2);
await handler.HandleAsync([message], CancellationToken.None);
```

## Target Framework

`net8.0`
