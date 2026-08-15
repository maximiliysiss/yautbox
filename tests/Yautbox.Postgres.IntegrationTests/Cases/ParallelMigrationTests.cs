using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Xunit;
using Xunit.Abstractions;
using Yautbox.Extensions.Ioc;
using Yautbox.Infrastructure.Waiter;
using Yautbox.Postgres.Extensions;
using Yautbox.Postgres.Extensions.Configurator;
using Yautbox.Postgres.IntegrationTests.Shared.Fixture;

namespace Yautbox.Postgres.IntegrationTests.Cases;

[Collection(nameof(IntegrationTestCollection))]
public sealed class ParallelMigrationTests(IntegrationTestFixture fixture, ITestOutputHelper testOutputHelper)
{
    private const int PodsCount = 5;
    private readonly string _connectionString = fixture.Services
        .GetRequiredService<IConfiguration>()
        .GetConnectionString("Outbox")
        ?? throw new InvalidOperationException("Connection string 'Outbox' is not configured.");

    [Fact(Timeout = 120_000)]
    public async Task Starting_N_Pods_Concurrently_Applies_Migrations_Once_And_All_Become_Ready()
    {
        var schema = $"migration_test_{Guid.NewGuid():N}";
        var pods = Enumerable.Range(0, PodsCount).Select(_ => CreatePod(schema)).ToArray();

        try
        {
            var started = Stopwatch.GetTimestamp();
            await Task.WhenAll(pods.Select(pod => pod.StartAsync()));
            await Task.WhenAll(pods.Select(pod => pod.Services
                .GetRequiredService<IInfrastructureReadinessWaiter>()
                .WaitAsync(CancellationToken.None)
                .WaitAsync(TimeSpan.FromMinutes(1))));

            foreach (var pod in pods)
                pod.Services.GetRequiredService<IInfrastructureReadinessWaiter>()
                    .WaitAsync(CancellationToken.None).IsCompletedSuccessfully.Should().BeTrue();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"""
                SELECT
                    (SELECT max(version) FROM {schema}.version_info),
                    to_regclass('{schema}.outbox_messages') IS NOT NULL,
                    to_regclass('{schema}.idx__outbox_messages_active__type_id_scheduled_at_coalesce') IS NOT NULL
                """, connection);
            await using var reader = await command.ExecuteReaderAsync();
            (await reader.ReadAsync()).Should().BeTrue();
            reader.GetInt64(0).Should().Be(6);
            reader.GetBoolean(1).Should().BeTrue();
            reader.GetBoolean(2).Should().BeTrue();

            var elapsed = Stopwatch.GetElapsedTime(started);
            testOutputHelper.WriteLine($"{PodsCount} pods migrated and became ready in {elapsed}.");
        }
        finally
        {
            await Task.WhenAll(pods.Select(pod => pod.StopAsync()));
            foreach (var pod in pods) pod.Dispose();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"DROP SCHEMA IF EXISTS {schema} CASCADE", connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    private IHost CreatePod(string schema) => Host.CreateDefaultBuilder()
        .ConfigureLogging(logging => logging.ClearProviders())
        .ConfigureServices(services => services.AddOutbox(builder =>
            builder.UsePostgres(_connectionString, new PostgresStoreOptions { SchemaName = schema })))
        .Build();
}
