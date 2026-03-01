# Yautbox.Postgres

PostgreSQL infrastructure provider for Yautbox. It stores outbox messages in Postgres, applies schema migrations on startup, and supports distributed locks for sequential execution.

## Features

- Postgres storage with schema migrations
- Advisory locks for `ExecutionPolicy.Sequential`
- Configurable schema name and JSON serialization
- Cleanup batching for safe-deleted records

## Installation

NuGet (if published):

```bash
dotnet add package Yautbox.Postgres
```

From source:

```bash
dotnet add <your-app>.csproj reference src/Yautbox.Postgres/Yautbox.Postgres.csproj
```

## Quick start

```csharp
using Microsoft.Extensions.DependencyInjection;
using Yautbox.Extensions.Ioc;
using Yautbox.Postgres.Extensions;

services.AddOutbox(builder => builder.UsePostgres(
    connectionString: "Host=localhost;Database=yautbox;Username=postgres;Password=postgres"));

services.AddOutboxHandler<OrderPlaced, OrderPlacedHandler>();
```

## Options

`PostgresStoreOptions`:

- `SchemaName` (default: "outbox", schema must already exist)
- `CleanupBatchSize` (default: 1000)
- `ConfigureJsonOptions` (customize `JsonSerializerOptions`)

`CleanupBatchSize` applies when `DeletePolicy.Safe` is used and cleanup is enabled via `BackupInterval`.

Example:

```csharp
using System.Text.Json;
using Yautbox.Postgres.Extensions;
using Yautbox.Postgres.Extensions.Configurator;

services.AddOutbox(builder => builder.UsePostgres(
    connectionString: "...",
    options: new PostgresStoreOptions
    {
        SchemaName = "messaging",
        CleanupBatchSize = 2000,
        ConfigureJsonOptions = (json, _) => json.PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    }));
```

## Connection factory

If you need custom connection creation (for example, custom connection pooling), register your own factory and use the generic overload:

```csharp
using Yautbox.Postgres.Extensions;
using Yautbox.Postgres.Infrastructure.Database;

public sealed class MyConnectionFactory : IOutboxConnectionFactory
{
    public string GetConnectionString() => "...";
    public Task<DbConnection> GetConnectionAsync(CancellationToken ct) => /* ... */;
}

services.AddOutbox(builder => builder.UsePostgres<MyConnectionFactory>());
```

## Migrations and readiness

This provider runs FluentMigrator migrations on startup and waits for readiness before handlers begin polling. Ensure the schema exists (or set `SchemaName` to an existing schema such as `public`). The database user must have permissions to create tables.

## Notes

- `ExecutionPolicy.Sequential` uses a Postgres advisory lock to ensure single active processing per identifier.
- `DeletePolicy.Safe` marks records as deleted; cleanup is performed by the background cleaner when `BackupInterval` is set.
