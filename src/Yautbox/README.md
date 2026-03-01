# Yautbox

Yautbox is a lightweight .NET outbox library. It lets you enqueue messages during application work and processes them later with background handlers. The core package is storage-agnostic; choose an infrastructure provider (InMemory, MSSQL, MySQL, Postgres) or implement your own.

## Features

- Outbox pattern with background processing
- Typed handlers with retry support
- Scheduled messages and visibility timeouts
- Configurable concurrency, polling, and cleanup
- Pluggable storage via `IOutboxProvider`

## Installation

NuGet (if published):

```bash
dotnet add package Yautbox
```

From source:

```bash
dotnet add <your-app>.csproj reference src/Yautbox/Yautbox.csproj
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

## Configuration options

Each handler can be configured via `AddOutboxHandler().ConfigureOptions<TOptions>()`. The default options type is `DefaultRunnerOptions`.

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

`IOutboxRunnerOptions` settings:

- `Identifier` (default: payload type assembly-qualified name without version, culture, or public key token)
- `PollDelay` (default: 5s + jitter)
- `BufferSize` (default: 1000)
- `PerBufferCount` (default: BufferSize)
- `HandleTimeout` (default: 30m)
- `IsEnabled` (default: true)
- `WorkersCount` (default: 1)
- `DeletePolicy` (default: Safe)
- `FailureDelay` (default: 2s + jitter)
- `Visibility` (default: 10m)
- `BackupInterval` (default: null, disabled)
- `ExecutionPolicy` (default: Parallel)
- `CancellationPolicy` (default: Safe)

## How it works

- `IOutboxService` enqueues messages into an `IOutboxProvider`.
- `AddOutboxHandler<TPayload, THandler>()` registers two hosted services:
  - a handler runner that polls, locks, and dispatches messages
  - a cleanup runner that deletes old handled records when `BackupInterval` is set
- `ExecutionPolicy.Sequential` uses a provider-level lock to ensure a single worker across processes.

## Extensibility

To create a custom storage implementation, implement `IOutboxProvider` and register it via `AddOutbox(builder => builder.SetProvider<...>())`.

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

## Target frameworks

`net8.0`
