using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using Yautbox.Entities;
using Yautbox.Infrastructure.Waiter;
using Yautbox.Postgres.Infrastructure.Database;
using Yautbox.Postgres.Infrastructure.DateTime;
using Yautbox.Postgres.IntegrationTests.DbHelper;
using Yautbox.Postgres.IntegrationTests.DbHelper.Shared.Extensions;
using Yautbox.Postgres.IntegrationTests.Shared.Extensions;
using Yautbox.Postgres.IntegrationTests.Shared.Fixture;
using Yautbox.Postgres.Options;
using Yautbox.Postgres.Repositories;
using Yautbox.Runner.Options;

namespace Yautbox.Postgres.IntegrationTests.Repository;

[Collection(nameof(IntegrationTestCollection))]
public class OutboxRepositoryTests : IAsyncLifetime
{
    private const string Identifier = nameof(TestEvent);

    private readonly IntegrationTestFixture _fixture;

    private readonly OutboxDbHelper _outboxDbHelper;

    public OutboxRepositoryTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;

        var mapper = _fixture.Services.GetRequiredService<IOutboxConnectionFactory>();
        var options = _fixture.Services.GetRequiredService<IOptions<PostgresOutboxRepositoryOptions>>();

        _outboxDbHelper = new OutboxDbHelper(mapper, options);
    }

    [Theory, AutoData]
    public async Task AddAsync_ShouldAddNewRecord(TestEvent @event)
    {
        // Arrange
        var repository = Create();

        var outboxMessage = new OutboxMessage<TestEvent>(
            Id: OutboxMessageId.Empty,
            Payload: @event,
            Attempt: 0,
            ScheduledAt: null,
            CreatedAt: DateTimeOffset.UtcNow);

        // Act
        var outboxMessageIds = await repository
            .AddAsync(identifier: Identifier, messages: [outboxMessage], cancellationToken: CancellationToken.None)
            .ToArrayAsync();

        _outboxDbHelper.Track(outboxMessageIds);

        // Assert
        outboxMessageIds.Should().ContainSingle();

        var outboxMessageId = outboxMessageIds.First();

        var expectedMessage = new
        {
            Id = outboxMessageId.Value,
            Type = Identifier,
            IsDeleted = false,
        };

        var expectedPayload = JsonSerializer.Serialize(@event);

        var tableRows = await _outboxDbHelper
            .GetAsync<TestEvent>(Identifier, outboxMessageId.Value)
            .ToArrayAsync();

        tableRows
            .Should().ContainSingle()
            .Which.Should().BeEquivalentTo(expectedMessage).And
            .BeOfType<OutboxDbHelper.TableRow>().Which.Payload.Should().BeJsonEquivalentTo(expectedPayload);
    }

    [Theory, AutoData]
    public async Task AddAsync_ShouldAddNewRecord_WhenEventHasPersonalData(PersonalTestEvent @event)
    {
        // Arrange
        var repository = Create();

        var outboxMessage = new OutboxMessage<PersonalTestEvent>(
            Id: OutboxMessageId.Empty,
            Payload: @event,
            Attempt: 0,
            ScheduledAt: null,
            CreatedAt: DateTimeOffset.UtcNow);

        // Act
        var outboxMessageIds = await repository
            .AddAsync(identifier: Identifier, messages: [outboxMessage], cancellationToken: CancellationToken.None)
            .ToArrayAsync();

        _outboxDbHelper.Track(outboxMessageIds);

        // Assert
        outboxMessageIds.Should().ContainSingle();

        var outboxMessageId = outboxMessageIds.First();

        var expectedMessage = new
        {
            Id = outboxMessageId.Value,
            Type = Identifier,
            IsDeleted = false,
        };

        var expectedPayload = JsonSerializer.Serialize(@event);

        var tableRows = await _outboxDbHelper
            .GetAsync<PersonalTestEvent>(Identifier, outboxMessageId.Value)
            .ToArrayAsync();

        tableRows
            .Should().ContainSingle()
            .Which.Should().BeEquivalentTo(expectedMessage).And
            .BeOfType<OutboxDbHelper.TableRow>().Which.Payload.Should().NotBeJsonEquivalentTo(expectedPayload);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteExistsRecord()
    {
        // Arrange
        var tableRow = OutboxDbHelper.TableRow.GetDefault(Identifier);

        var id = await _outboxDbHelper.AddAsync(tableRow);

        var repository = Create();

        // Act
        await repository.DeleteAsync(
            ids: [new OutboxMessageId(id)],
            policy: DeletePolicy.Delete,
            cancellationToken: CancellationToken.None);

        // Assert
        var tableRows = await _outboxDbHelper
            .GetAsync<TestEvent>(Identifier, id)
            .ToArrayAsync();

        var expectedMessage = new
        {
            Id = id,
            Type = tableRow.Type,
            IsDeleted = true,
        };

        tableRows
            .Should().ContainSingle()
            .Which.Should().BeEquivalentTo(expectedMessage);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnEmpty_WhenThereIsNoRecord()
    {
        // Arrange
        var repository = Create();

        // Act
        var messages = await repository
            .GetAsync<TestEvent>(
                identifier: Identifier,
                count: 1,
                locker: TimeSpan.FromMinutes(1),
                cancellationToken: CancellationToken.None)
            .ToArrayAsync();

        // Assert
        messages.Should().BeEmpty();
    }

    [Theory, AutoData]
    public async Task GetAsync_ShouldReturnRecord(TestEvent @event)
    {
        // Arrange
        var tableRow = OutboxDbHelper.TableRow.GetDefault(Identifier, JsonSerializer.Serialize(@event));

        var id = await _outboxDbHelper.AddAsync(tableRow);

        var repository = Create();

        // Act
        var messages = await repository
            .GetAsync<TestEvent>(
                identifier: Identifier,
                count: 1,
                locker: TimeSpan.FromMinutes(1),
                cancellationToken: CancellationToken.None)
            .ToArrayAsync();

        // Assert
        var expected = new
        {
            Id = new OutboxMessageId(id),
            Payload = @event
        };

        messages
            .Should().ContainSingle()
            .Which.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetAsync_ShouldReturn_WhenGetItByPages()
    {
        // Arrange
        var testEvents = Enumerable.Range(1, 10)
            .Select(i => new TestEvent(i, $"Name {i}"))
            .ToArray();

        var tableRows = testEvents
            .Select(c => OutboxDbHelper.TableRow.GetDefault(Identifier, JsonSerializer.Serialize(c)))
            .ToArray();

        foreach (var tableRow in tableRows)
            await _outboxDbHelper.AddAsync(tableRow);

        tableRows = tableRows
            .OrderBy(c => c.Id)
            .ToArray();

        var repository = Create();

        // Act
        var firstPart = await repository
            .GetAsync<TestEvent>(
                identifier: Identifier,
                count: 3,
                locker: TimeSpan.FromMinutes(1),
                cancellationToken: CancellationToken.None)
            .ToArrayAsync();

        var secondPart = await repository
            .GetAsync<TestEvent>(
                identifier: Identifier,
                count: 3,
                locker: TimeSpan.FromMinutes(1),
                cancellationToken: CancellationToken.None)
            .ToArrayAsync();

        var thirdPart = await repository
            .GetAsync<TestEvent>(
                identifier: Identifier,
                count: 5,
                locker: TimeSpan.FromMinutes(1),
                cancellationToken: CancellationToken.None)
            .ToArrayAsync();

        // Assert
        firstPart
            .Should().HaveCount(3).And
            .BeEquivalentTo(tableRows.Take(3).Select(Map));

        secondPart
            .Should().HaveCount(3)
            .And.BeEquivalentTo(tableRows.Skip(3).Take(3).Select(Map));

        thirdPart
            .Should().HaveCount(4)
            .And.BeEquivalentTo(tableRows.Skip(6).Select(Map));

        return;

        static object Map(OutboxDbHelper.TableRow tableRow)
        {
            return new
            {
                Id = new OutboxMessageId(tableRow.Id),
                Payload = JsonSerializer.Deserialize<TestEvent>(tableRow.Payload),
            };
        }
    }

    [Fact]
    public async Task GetAsync_ShouldReturn_WhenGetItByPagesInParallel()
    {
        // Arrange
        var testEvents = Enumerable.Range(1, 42)
            .Select(i => new TestEvent(i, $"Name {i}"))
            .ToArray();

        var tableRows = testEvents
            .Select(c => OutboxDbHelper.TableRow.GetDefault(Identifier, JsonSerializer.Serialize(c)))
            .ToArray();

        foreach (var tableRow in tableRows)
            await _outboxDbHelper.AddAsync(tableRow);

        tableRows = tableRows
            .OrderBy(c => c.Id)
            .ToArray();

        var repository = Create();

        // Act
        var tasks = Enumerable.Range(1, testEvents.Length / 4 + 1)
            .Select(_ => repository
                .GetAsync<TestEvent>(
                    identifier: Identifier,
                    count: 4,
                    locker: TimeSpan.FromMinutes(5),
                    cancellationToken: CancellationToken.None)
                .ToArrayAsync()
                .AsTask());

        var outboxMessages = await Task.WhenAll(tasks);

        // Assert
        var expectedMessages = tableRows
            .Select(Map)
            .ToDictionary(c => c.Id, c => c.Payload);

        var all = outboxMessages.SelectMany(c => c).ToArray();
        all.Select(m => m.Id).Distinct().Should().HaveCount(all.Length, "no duplicates across parallel pages");
        all.Should().HaveCount(expectedMessages.Count, "all messages should be fetched exactly once");
        expectedMessages.Keys.Should().BeEquivalentTo(all.Select(m => m.Id), "full coverage of expected IDs");


        foreach (var outboxMessage in outboxMessages)
            outboxMessage.Length.Should().BeLessThanOrEqualTo(4);

        foreach (var outboxMessage in all)
        {
            expectedMessages.Keys.Should().Contain(outboxMessage.Id);
            expectedMessages[outboxMessage.Id].Should().BeEquivalentTo(outboxMessage.Payload);
        }

        return;

        static (OutboxMessageId Id, TestEvent? Payload) Map(OutboxDbHelper.TableRow tableRow)
        {
            return (Id: new OutboxMessageId(tableRow.Id), Payload: JsonSerializer.Deserialize<TestEvent>(tableRow.Payload));
        }
    }

    [Theory, AutoData]
    public async Task GetAsync_ShouldReturnRecordAndDecode(PersonalTestEvent @event)
    {
        // Arrange
        var repository = Create();

        var outboxMessage = new OutboxMessage<PersonalTestEvent>(
            Id: OutboxMessageId.Empty,
            Payload: @event,
            Attempt: 0,
            ScheduledAt: null,
            CreatedAt: DateTimeOffset.UtcNow);

        // Act
        var outboxMessageIds = await repository
            .AddAsync(Identifier, [outboxMessage], CancellationToken.None)
            .ToArrayAsync();

        _outboxDbHelper.Track(outboxMessageIds);

        var messages = await repository
            .GetAsync<PersonalTestEvent>(
                identifier: Identifier,
                count: 1,
                locker: TimeSpan.FromMinutes(1),
                cancellationToken: CancellationToken.None)
            .ToArrayAsync();

        // Assert
        var id = outboxMessageIds.Should().ContainSingle().Which;

        var expected = new
        {
            Id = id,
            Payload = @event
        };

        messages
            .Should().ContainSingle()
            .Which.Should().BeEquivalentTo(expected);
    }

    [Theory, AutoData]
    public async Task GetAsync_ShouldReturnEmpty_WhenRecordIsInFuture(TestEvent @event)
    {
        // Arrange
        var tableRow = OutboxDbHelper.TableRow
            .GetFaker(Identifier, JsonSerializer.Serialize(@event))
            .RuleFor(c => c.ScheduledAt, DateTimeOffset.UtcNow.AddDays(1))
            .Generate();

        _ = await _outboxDbHelper.AddAsync(tableRow);

        var repository = Create();

        // Act
        var messages = await repository
            .GetAsync<TestEvent>(
                identifier: Identifier,
                count: 1,
                locker: TimeSpan.FromMinutes(1),
                cancellationToken: CancellationToken.None)
            .ToArrayAsync();

        // Assert
        messages.Should().BeEmpty();
    }

    private IPostgresOutboxRepository Create()
    {
        var optionsSnapshot = Substitute.For<IOptionsSnapshot<PostgresOutboxRepositoryOptions>>();

        optionsSnapshot
            .Value
            .Returns(new PostgresOutboxRepositoryOptions { SchemaName = "outbox" });

        return new PostgresOutboxRepository(
            logger: NullLogger<PostgresOutboxRepository>.Instance,
            options: optionsSnapshot,
            dateTimeProvider: new DateTimeProvider(),
            connectionFactory: _fixture.Services.GetRequiredService<IOutboxConnectionFactory>());
    }

    public Task InitializeAsync()
        => _fixture.Services.GetRequiredService<IInfrastructureReadinessWaiter>().WaitAsync(CancellationToken.None);

    public async Task DisposeAsync() => await _outboxDbHelper.DisposeAsync();

    public sealed record TestEvent(int Id, string Name);

    public sealed record PersonalTestEvent(int Id, [property: PersonalData] string Name);
}
