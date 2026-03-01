using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Xunit;
using Yautbox.Entities;
using Yautbox.Mysql.Infrastructure.Database;
using Yautbox.Mysql.IntegrationTests.DbHelper;
using Yautbox.Mysql.IntegrationTests.Shared.Extensions;
using Yautbox.Mysql.IntegrationTests.Shared.Fixture;
using Yautbox.Mysql.Options;
using Yautbox.Mysql.Repositories;

namespace Yautbox.Mysql.IntegrationTests.Cases;

[Collection(nameof(IntegrationTestCollection))]
public class ConcurrentAccessTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;

    private readonly OutboxDbHelper _outboxDbHelper;

    public ConcurrentAccessTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;

        var mapper = _fixture.Services.GetRequiredService<IOutboxConnectionFactory>();
        var options = _fixture.Services.GetRequiredService<IOptions<MysqlOutboxRepositoryOptions>>();

        _outboxDbHelper = new OutboxDbHelper(mapper, options);
    }

    [Fact]
    public async Task GetAsync_ShouldWorkCorrect_WhenThereIsConcurrency()
    {
        // Arrange
        var identifier = typeof(TestMessage).GetVersionFreeFullName();

        var tableRow = OutboxDbHelper.TableRow.GetDefault(
            type: identifier,
            payload: JsonSerializer.Serialize(new TestMessage(1, "Test")));

        var id = await _outboxDbHelper.AddAsync(tableRow);

        using var serviceScope = _fixture.Services.CreateScope();
        var outboxRepository = serviceScope.ServiceProvider.GetRequiredService<IMysqlOutboxRepository>();

        var delay = TimeSpan.FromHours(1);

        // Act
        var tasks = Enumerable
            .Range(0, 50)
            .Select(_ => GetWithDeadlockRetryAsync(outboxRepository, identifier, delay, CancellationToken.None));

        var messages = await Task.WhenAll(tasks);

        // Assert
        messages.SelectMany(c => c)
            .Should().ContainSingle()
            .Which.Id.Should().Be(new OutboxMessageId(id));
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _outboxDbHelper.DisposeAsync().AsTask();

    private static async Task<OutboxMessage<TestMessage>[]> GetWithDeadlockRetryAsync(
        IMysqlOutboxRepository outboxRepository,
        string identifier,
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 5;
        var backoff = TimeSpan.FromMilliseconds(25);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await outboxRepository
                    .GetAsync<TestMessage>(identifier, count: 2, delay, cancellationToken)
                    .ToArrayAsync(cancellationToken: cancellationToken)
                    .AsTask();
            }
            catch (MySqlException ex) when (IsDeadlock(ex) && attempt < maxAttempts)
            {
                await Task.Delay(backoff, cancellationToken);
                backoff = TimeSpan.FromMilliseconds(Math.Min(backoff.TotalMilliseconds * 2, 500));
            }
        }

        return [];
    }

    private static bool IsDeadlock(MySqlException ex)
        => ex.ErrorCode == MySqlErrorCode.LockDeadlock || ex.Number == 1213;

    private sealed record TestMessage(int Id, string Name);
}
