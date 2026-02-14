using System;
using System.Threading;
using System.Threading.Tasks;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Processors;
using Medallion.Threading.Postgres;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Yautbox.Postgres.Infrastructure.Database;
using Yautbox.Postgres.Migrations.Options;

namespace Yautbox.Postgres.Migrations.Services;

internal sealed class OutboxMigrationRunner : IOutboxMigrationRunner
{
    private static readonly TimeSpan _defaultTimeout = TimeSpan.FromMinutes(10);

    private readonly MigrationOptions _options;

    private readonly IOutboxConnectionFactory _connectionFactory;

    private readonly ILoggerFactory _loggerFactory;

    public OutboxMigrationRunner(
        IOutboxConnectionFactory connectionFactory,
        IOptions<MigrationOptions> options,
        ILoggerFactory loggerFactory)
    {
        _connectionFactory = connectionFactory;
        _loggerFactory = loggerFactory;
        _options = options.Value;
    }

    public async Task MigrateUpAsync(CancellationToken cancellationToken)
    {
        var @lock = new PostgresDistributedLock(
            key: new PostgresAdvisoryLockKey(nameof(OutboxMigrationRunner), allowHashing: true),
            connectionString: _connectionFactory.GetConnectionString());

        await using var _ = await @lock.AcquireAsync(_defaultTimeout, cancellationToken: cancellationToken);

        // Create scope
        var assembly = typeof(InitialMigration).Assembly;

        await using var serviceProvider = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(builder => builder
                .AddPostgres()
                .ScanIn(assembly).For.All())
            .AddOptions<ProcessorOptions>()
            .Configure(options =>
            {
                options.ProviderSwitches = "Force Quote=false";
                options.Timeout = _defaultTimeout;
                options.ConnectionString = _connectionFactory.GetConnectionString();
            })
            .Services
            .AddOptions<MigrationOptions>()
            .Configure(opt => opt.SchemaName = _options.SchemaName)
            .Services
            .AddSingleton(_loggerFactory)
            .BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();

        // Migrate
        var runner = serviceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();

        // Reload types
        await using var connection = await _connectionFactory.GetConnectionAsync(cancellationToken);

        await using var npgsqlConnection = connection as NpgsqlConnection;

        if (npgsqlConnection is not null)
        {
            await npgsqlConnection.OpenAsync(cancellationToken);
            await npgsqlConnection.ReloadTypesAsync(cancellationToken);
        }
    }
}
