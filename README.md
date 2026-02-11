# Yautbox

Yautbox is a lightweight .NET outbox library. It lets you enqueue messages during application work and processes them
later with background handlers. The core package is storage-agnostic; choose an infrastructure provider (InMemory,
MSSQL, Postgres) or implement your own.

## Providers

| Provider | Package            | Documentation                            |
|----------|--------------------|------------------------------------------|
| InMemory | `Yautbox.InMemory` | [README](src/Yautbox.InMemory/README.md) |
| MSSQL    | `Yautbox.Mssql`    | [README](src/Yautbox.Mssql/README.md)    |
| Postgres | `Yautbox.Postgres` | [README](src/Yautbox.Postgres/README.md) |
