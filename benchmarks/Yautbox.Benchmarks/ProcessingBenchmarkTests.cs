using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Xunit;
using Xunit.Abstractions;
using Yautbox.Extensions.Ioc;
using Yautbox.Handlers;
using Yautbox.InMemory.Options;
using Yautbox.Infrastructure.Waiter;
using Yautbox.InMemory.Extensions;
using Yautbox.Postgres.Extensions;
using Yautbox.Postgres.Extensions.Configurator;
using Yautbox.Services;

namespace Yautbox.Benchmarks;

[Trait("Category", "Benchmark")]
public sealed class ProcessingBenchmarkTests(ITestOutputHelper output)
{
    private static long _handled;

    private static readonly string _connectionString = Environment.GetEnvironmentVariable("YAUTBOX_POSTGRES")
                                                       ??
                                                       "Host=localhost;Database=postgres;Username=postgres;Password=pwd;Port=5432;Pooling=true;Maximum Pool Size=200";

    [Fact(Timeout = 3_600_000)]
    public async Task Providers_Process_Backlog_In_Different_Configurations()
    {
        var postgresMessages = ReadPositiveInt("YAUTBOX_POSTGRES_MESSAGES", 2_000_000);
        var inMemoryMessages = ReadPositiveInt("YAUTBOX_INMEMORY_MESSAGES", 250_000);
        var scenarios = new[]
        {
            new Scenario("Postgres", postgresMessages, 1, 2, 2_000, 500),
            new Scenario("Postgres", postgresMessages, 3, 2, 2_000, 500),
            new Scenario("Postgres", postgresMessages, 5, 4, 5_000, 1_000),
            new Scenario("InMemory", inMemoryMessages, 1, 1, 2_000, 500),
            new Scenario("InMemory", inMemoryMessages, 1, 4, 5_000, 1_000)
        };

        var results = new List<BenchmarkResult>();
        try
        {
            foreach (var scenario in scenarios)
                results.Add(
                    scenario.Provider == "Postgres"
                        ? await RunPostgresAsync(scenario)
                        : await RunInMemoryAsync(scenario));
        }
        finally
        {
            if (results.Count > 0)
                BenchmarkReport.Write(results, output);
        }
    }

    private static async Task<BenchmarkResult> RunPostgresAsync(Scenario scenario)
    {
        var schema = $"benchmark_{Guid.NewGuid():N}";
        try
        {
            using (var migrator = CreateHost("Postgres", schema, scenario))
            {
                await migrator.StartAsync();
                await migrator.Services.GetRequiredService<IInfrastructureReadinessWaiter>()
                    .WaitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromMinutes(10));
                await migrator.StopAsync();
            }

            await SeedPostgresAsync(schema, scenario.Messages);
            Interlocked.Exchange(ref _handled, 0);

            var pods = Enumerable.Range(0, scenario.Pods)
                .Select(_ => CreateHost("Postgres", schema, scenario))
                .ToArray();
            var started = Stopwatch.GetTimestamp();
            try
            {
                await Task.WhenAll(pods.Select(pod => pod.StartAsync()));
                await WaitUntilAsync(() => Volatile.Read(ref _handled) >= scenario.Messages, TimeSpan.FromMinutes(30));
                await WaitUntilAsync(async () => await CountActiveAsync(schema) == 0, TimeSpan.FromMinutes(1));
                var elapsed = Stopwatch.GetElapsedTime(started);

                Volatile.Read(ref _handled).Should().Be(scenario.Messages, "each claimed message must be handled exactly once");
                (await CountActiveAsync(schema)).Should().Be(0);
                return scenario.ToResult(elapsed);
            }
            finally
            {
                await Task.WhenAll(pods.Select(pod => pod.StopAsync()));
                foreach (var pod in pods) pod.Dispose();
            }
        }
        finally
        {
            await DropSchemaAsync(schema);
        }
    }

    private static async Task<BenchmarkResult> RunInMemoryAsync(Scenario scenario)
    {
        Interlocked.Exchange(ref _handled, 0);
        using var host = CreateHost("InMemory", string.Empty, scenario);
        await host.StartAsync();
        var started = Stopwatch.GetTimestamp();
        try
        {
            using var scope = host.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IOutboxService>();
            foreach (var chunk in Enumerable.Range(0, scenario.Messages).Chunk(10_000))
                await service.HandleAsync(chunk.Select(id => new BenchmarkMessage(id)));

            await WaitUntilAsync(() => Volatile.Read(ref _handled) >= scenario.Messages, TimeSpan.FromMinutes(15));
            var elapsed = Stopwatch.GetElapsedTime(started);
            Volatile.Read(ref _handled).Should().Be(scenario.Messages);
            return scenario.ToResult(elapsed);
        }
        finally
        {
            await host.StopAsync();
        }
    }

    private static IHost CreateHost(string provider, string schema, Scenario scenario) => Host
        .CreateDefaultBuilder()
        .ConfigureLogging(logging => logging.ClearProviders())
        .ConfigureServices(services =>
        {
            services.AddSingleton(
                new BenchmarkRunnerOptions
                {
                    WorkersCount = scenario.Workers,
                    BufferSize = scenario.Buffer,
                    PerBufferCount = scenario.Batch
                });
            services.AddOutbox(builder =>
            {
                if (provider == "Postgres")
                    builder.UsePostgres(_connectionString, new PostgresStoreOptions { SchemaName = schema });
                else
                    builder.UseInMemory(new InMemoryOutboxOptions { Capacity = Math.Max(scenario.Messages, 10_000) });
            });
            services.AddOutboxHandler<BenchmarkMessage, CountingHandler>()
                .ConfigureOptions<BenchmarkRunnerOptions>();
        })
        .Build();

    private static async Task SeedPostgresAsync(string schema, int count)
    {
        var identifier = $"{typeof(BenchmarkMessage).FullName}, {typeof(BenchmarkMessage).Assembly.GetName().Name}";
        var payload = JsonSerializer.Serialize(new BenchmarkMessage(0));
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            $$"""
              INSERT INTO {{schema}}.outbox_messages(type, payload, created_at, attempt, scheduled_at, is_deleted)
              SELECT @type, jsonb_set(@payload::jsonb, '{Id}', to_jsonb(id)), now(), 0, NULL, false
              FROM generate_series(1, @count) AS id;
              """,
            connection);
        command.Parameters.AddWithValue("type", identifier);
        command.Parameters.AddWithValue("payload", payload);
        command.Parameters.AddWithValue("count", count);
        command.CommandTimeout = 600;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<long> CountActiveAsync(string schema)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"SELECT count(*) FROM {schema}.outbox_messages_active", connection);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private static async Task DropSchemaAsync(string schema)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP SCHEMA IF EXISTS {schema} CASCADE", connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var started = Stopwatch.GetTimestamp();
        while (!condition())
        {
            if (Stopwatch.GetElapsedTime(started) > timeout)
                throw new TimeoutException($"The benchmark did not finish in {timeout}.");
            await Task.Delay(100);
        }
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var started = Stopwatch.GetTimestamp();
        while (!await condition())
        {
            if (Stopwatch.GetElapsedTime(started) > timeout)
                throw new TimeoutException($"The benchmark did not finish in {timeout}.");
            await Task.Delay(100);
        }
    }

    private static int ReadPositiveInt(string name, int fallback) =>
        int.TryParse(Environment.GetEnvironmentVariable(name), NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : fallback;

    public sealed record BenchmarkMessage(int Id);

    public sealed class CountingHandler : IOutboxHandler<BenchmarkMessage>
    {
        public Task HandleAsync(IEnumerable<OutboxMessage<BenchmarkMessage>> messages, CancellationToken cancellationToken)
        {
            Interlocked.Add(ref _handled, messages.Count());
            return Task.CompletedTask;
        }
    }

    private sealed record Scenario(string Provider, int Messages, int Pods, int Workers, int Buffer, int Batch)
    {
        public BenchmarkResult ToResult(TimeSpan elapsed) => new(Provider, Messages, Pods, Workers, Buffer, Batch, elapsed);
    }
}
