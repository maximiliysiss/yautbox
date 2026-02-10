using System;
using System.Threading;
using System.Threading.Tasks;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Processors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yautbox.Mssql.Infrastructure.Database;
using Yautbox.Mssql.Migrations.Options;

namespace Yautbox.Mssql.Migrations.Services;

internal sealed class OutboxMigrationRunner(
    IOutboxConnectionFactory connectionFactory,
    IOptions<MigrationOptions> options,
    ILoggerFactory loggerFactory)
    : IOutboxMigrationRunner
{
    private readonly MigrationOptions _options = options.Value;

    public async Task MigrateUpAsync(CancellationToken cancellationToken)
    {
        var assembly = typeof(InitialMigration).Assembly;

        await using var serviceProvider = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(builder => builder
                .AddSqlServer()
                .ScanIn(assembly).For.All())
            .AddOptions<ProcessorOptions>()
            .Configure(options =>
            {
                options.Timeout = TimeSpan.FromMinutes(10);
                options.ConnectionString = connectionFactory.GetConnectionString();
            })
            .Services
            .AddOptions<MigrationOptions>()
            .Configure(opt => opt.SchemaName = _options.SchemaName)
            .Services
            .AddSingleton(loggerFactory)
            .BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();

        var runner = serviceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();

        await Task.CompletedTask;
    }
}
