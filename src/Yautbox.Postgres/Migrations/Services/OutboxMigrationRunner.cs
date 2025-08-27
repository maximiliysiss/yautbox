using Yautbox.Postgres.Migrations.Options;

namespace Yautbox.Postgres.Migrations.Services;

public sealed class OutboxMigrationRunner(
    IOutboxConnectionFactory connectionFactory,
    IOptions<MigrationOptions> options,
    ILoggerFactory loggerFactory)
    : IOutboxMigrationRunner
{
    private readonly MigrationOptions _options = options.Value;

    public async Task MigrateUpAsync(CancellationToken cancellationToken)
    {
        // Create scope
        var assembly = typeof(InitialMigration).Assembly;

        await using var serviceProvider = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(
                builder => builder
                    .AddPostgres()
                    .ScanIn(assembly).For.All())
            .AddOptions<ProcessorOptions>()
            .Configure(
                options =>
                {
                    options.ProviderSwitches = "Force Quote=false";
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

        // Migrate
        var runner = serviceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();

        // Reload types
        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);
        await using var npgsqlConnection = (NpgsqlConnection)connection;

        await npgsqlConnection.OpenAsync(cancellationToken);
        await npgsqlConnection.ReloadTypesAsync();
    }
}
