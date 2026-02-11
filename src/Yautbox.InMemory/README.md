# Yautbox.InMemory

In-memory infrastructure provider for Yautbox. This provider stores outbox messages in-process using a bounded queue. It is ideal for tests, local development, or ephemeral workloads where durability is not required.

## Features

- In-process queue with capacity limits
- Visibility timeout and rescheduling
- Optional scheduling via `scheduledAt`
- Transaction-aware enqueue when `System.Transactions` is used

## Installation

NuGet (if published):

```bash
dotnet add package Yautbox.InMemory
```

From source:

```bash
dotnet add <your-app>.csproj reference src/Yautbox.InMemory/Yautbox.InMemory.csproj
```

## Quick start

```csharp
using Microsoft.Extensions.DependencyInjection;
using Yautbox.Extensions.Ioc;
using Yautbox.InMemory.Extensions;

services.AddOutbox(builder => builder.UseInMemory());
```

You still register handlers in the core package:

```csharp
using Yautbox.Extensions.Ioc;

services.AddOutboxHandler<OrderPlaced, OrderPlacedHandler>();
```

## Options

`InMemoryOutboxOptions`:

- `Capacity` (default: 10000 per handler)

Example:

```csharp
using Yautbox.InMemory.Extensions;
using Yautbox.InMemory.Options;

services.AddOutbox(builder => builder.UseInMemory(new InMemoryOutboxOptions
{
    Capacity = 50000
}));
```

## Notes

- Messages are stored only in memory and are lost on process restart.
- Visibility and retry logic is implemented in-memory and should not be used as a durability mechanism in production.
