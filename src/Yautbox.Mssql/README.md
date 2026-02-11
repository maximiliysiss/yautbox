# Yautbox.Mssql

SQL Server infrastructure provider for Yautbox. It stores outbox messages in MSSQL, applies schema migrations on startup, and supports distributed locks for sequential execution.

## Features

- SQL Server storage with schema migrations
- Distributed lock for `ExecutionPolicy.Sequential`
- Configurable schema name and JSON serialization
- Cleanup batching for safe-deleted records

## Installation

NuGet (if published):

```bash
dotnet add package Yautbox.Mssql
```

From source:

```bash
dotnet add <your-app>.csproj reference src/Yautbox.Mssql/Yautbox.Mssql.csproj
```

## Quick start

```csharp
using Microsoft.Extensions.DependencyInjection;
using Yautbox.Extensions.Ioc;
using Yautbox.Mssql.Extensions;

services.AddOutbox(builder => builder.UseMssql(
    connectionString: "Server=.;Database=yautbox;Trusted_Connection=True;"));

services.AddOutboxHandler<OrderPlaced, OrderPlacedHandler>();
```

## Options

`MssqlStoreOptions`:

- `SchemaName` (default: "outbox")
- `CleanupBatchSize` (default: 1000)
- `ConfigureJsonOptions` (customize `JsonSerializerOptions`)

Example:

```csharp
using System.Text.Json;
using Yautbox.Mssql.Extensions;
using Yautbox.Mssql.Extensions.Configurator;

services.AddOutbox(builder => builder.UseMssql(
    connectionString: "...",
    options: new MssqlStoreOptions
    {
        SchemaName = "messaging",
        CleanupBatchSize = 2000,
        ConfigureJsonOptions = (json, _) => json.PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    }));
```

## Connection factory

If you need custom connection creation (for example, Azure SQL with custom tokens), register your own factory and use the generic overload:

```csharp
using Yautbox.Mssql.Extensions;
using Yautbox.Mssql.Infrastructure.Database;

public sealed class MyConnectionFactory : IOutboxConnectionFactory
{
    public string GetConnectionString() => "...";
    public Task<DbConnection> GetConnectionAsync(CancellationToken ct) => /* ... */;
}

services.AddOutbox(builder => builder.UseMssql<MyConnectionFactory>());
```

## Migrations and readiness

This provider runs FluentMigrator migrations on startup and waits for readiness before handlers begin polling. The database user must have permissions to create schemas, tables, and types.

## Notes

- `ExecutionPolicy.Sequential` uses a SQL Server distributed lock to ensure single active processing per identifier.
- `DeletePolicy.Safe` marks records as deleted; cleanup is performed by the background cleaner when `BackupInterval` is set.
