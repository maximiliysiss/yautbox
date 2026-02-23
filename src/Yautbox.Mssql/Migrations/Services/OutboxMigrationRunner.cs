using System;
using System.Threading;
using System.Threading.Tasks;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.Processors;
using FluentMigrator.Runner.VersionTableInfo;
using Medallion.Threading.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yautbox.Mssql.Infrastructure.Database;
using Yautbox.Mssql.Migrations.Configuration;
using Yautbox.Mssql.Migrations.Infrastructure;
using Yautbox.Mssql.Migrations.Options;
using Yautbox.Postgres.Migrations.Infrastructure;

namespace Yautbox.Mssql.Migrations.Services;

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
        var @lock = new SqlDistributedLock(
            name: nameof(OutboxMigrationRunner),
            connectionString: _connectionFactory.GetConnectionString());

        await using var _ = await @lock.AcquireAsync(_defaultTimeout, cancellationToken: cancellationToken);

        var assembly = typeof(InitialMigration).Assembly;

        await using var serviceProvider = new ServiceCollection()
            .AddScoped<IVersionTableMetaData, VersionTableMetaData>()
            .AddScoped<IVersionTableMetaDataAccessor, VersionTableMetaDataAccessor>()
            .AddSingleton<IMigrationSourceItem, MigrationSourceItem>()
            .AddFluentMigratorCore()
            .ConfigureRunner(builder => builder.AddSqlServer())
            .AddOptions<ProcessorOptions>()
            .Configure(options =>
            {
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

        var runner = serviceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();

        await Task.CompletedTask;
    }
}
