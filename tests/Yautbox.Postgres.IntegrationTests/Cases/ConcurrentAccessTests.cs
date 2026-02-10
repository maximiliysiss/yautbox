using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using Yautbox.Entities;
using Yautbox.Postgres.Infrastructure.Database;
using Yautbox.Postgres.IntegrationTests.DbHelper;
using Yautbox.Postgres.IntegrationTests.Shared.Extensions;
using Yautbox.Postgres.IntegrationTests.Shared.Fixture;
using Yautbox.Postgres.Options;
using Yautbox.Postgres.Repositories;

namespace Yautbox.Postgres.IntegrationTests.Cases;

[Collection(nameof(IntegrationTestCollection))]
public class ConcurrentAccessTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;

    private readonly OutboxDbHelper _outboxDbHelper;

    public ConcurrentAccessTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;

        var mapper = _fixture.Services.GetRequiredService<IOutboxConnectionFactory>();
        var options = _fixture.Services.GetRequiredService<IOptions<PostgresOutboxRepositoryOptions>>();

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
        var outboxRepository = serviceScope.ServiceProvider.GetRequiredService<IPostgresOutboxRepository>();

        var delay = TimeSpan.FromHours(1);

        // Act
        var tasks = Enumerable
            .Range(0, 50)
            .Select(_ => outboxRepository
                .GetAsync<TestMessage>(identifier, count: 2, delay, CancellationToken.None)
                .ToArrayAsync()
                .AsTask());

        var messages = await Task.WhenAll(tasks);

        // Assert
        messages.SelectMany(c => c)
            .Should().ContainSingle()
            .Which.Id.Should().Be(new OutboxMessageId(id));
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _outboxDbHelper.DisposeAsync().AsTask();

    private sealed record TestMessage(int Id, string Name);
}
