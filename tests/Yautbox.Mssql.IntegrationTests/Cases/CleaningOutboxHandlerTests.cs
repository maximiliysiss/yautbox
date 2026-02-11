using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Xunit;
using Yautbox.Handlers;
using Yautbox.Mssql.Infrastructure.Database;
using Yautbox.Mssql.IntegrationTests.DbHelper;
using Yautbox.Mssql.IntegrationTests.Shared.Fixture;
using Yautbox.Mssql.Options;
using Yautbox.Services;

namespace Yautbox.Mssql.IntegrationTests.Cases;

[Collection(nameof(IntegrationTestCollection))]
public class CleaningOutboxHandlerTests
{
    private readonly IntegrationTestFixture _fixture;

    private readonly OutboxDbHelper _outboxDbHelper;

    public CleaningOutboxHandlerTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;

        var mapper = _fixture.Services.GetRequiredService<IOutboxConnectionFactory>();
        var options = _fixture.Services.GetRequiredService<IOptions<MssqlOutboxRepositoryOptions>>();

        _outboxDbHelper = new OutboxDbHelper(mapper, options);
    }

    [Fact]
    public async Task CleanerRunner_ShouldWork()
    {
        // Arrange
        Handler.Reset();

        var messages = Enumerable
            .Range(1, 100)
            .Select(i => new Message(i))
            .ToArray();

        using var scope = _fixture.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IOutboxService>();

        // Act
        await service.HandleAsync(messages);

        // Assert
        var records = await Polly.Policy
            .HandleResult<OutboxDbHelper.TableRow[]>(i => i is not [])
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
            .ExecuteAsync(async () => await _outboxDbHelper.GetAsync<Message>().ToArrayAsync());

        records.Should().BeEmpty();

        Handler.CallCount.Should().Be(messages.Length);
    }

    public sealed record Message(int Value);

    public sealed class Handler : IOutboxHandler<Message>
    {
        private static int _callCount;
        public static int CallCount => Volatile.Read(ref _callCount);

        public static void Reset() => Interlocked.Exchange(ref _callCount, 0);

        public Task HandleAsync(IEnumerable<OutboxMessage<Message>> messages, CancellationToken cancellationToken)
        {
            foreach (var _ in messages)
                Interlocked.Increment(ref _callCount);

            return Task.CompletedTask;
        }
    }
}
