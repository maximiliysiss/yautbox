using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Xunit;
using Yautbox.Handlers;
using Yautbox.Mysql.Infrastructure.Database;
using Yautbox.Mysql.IntegrationTests.DbHelper;
using Yautbox.Mysql.IntegrationTests.Shared.Fixture;
using Yautbox.Mysql.Options;
using Yautbox.Services;

namespace Yautbox.Mysql.IntegrationTests.Cases;

[Collection(nameof(IntegrationTestCollection))]
public class RetryOutboxTests
{
    private readonly IntegrationTestFixture _fixture;

    private readonly OutboxDbHelper _dbHelper;

    public RetryOutboxTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;

        var mapper = _fixture.Services.GetRequiredService<IOutboxConnectionFactory>();
        var options = _fixture.Services.GetRequiredService<IOptions<MysqlOutboxRepositoryOptions>>();

        _dbHelper = new OutboxDbHelper(mapper, options);
    }

    [Theory, AutoData]
    public async Task Handle_ShouldSendMessageAndHandleAsync(RetryOutboxEvent message)
    {
        // Arrange
        using var serviceScope = _fixture.Services.CreateScope();

        var outboxService = serviceScope.ServiceProvider.GetRequiredService<IOutboxService>();

        // Act
        await outboxService.HandleAsync(
            messages: [message],
            cancellationToken: CancellationToken.None);

        // Assert
        var counter = await Polly.Policy
            .HandleResult<IEnumerable<OutboxDbHelper.TableRow>>(i => i.All(c => c.Attempt is 0))
            .WaitAndRetryAsync(3, a => TimeSpan.FromSeconds(Math.Min(10, a * a)))
            .ExecuteAsync(async () => await _dbHelper.GetAsync<RetryOutboxEvent>().ToArrayAsync());

        counter
            .Should().ContainSingle()
            .Which.Attempt.Should().Be(1);
    }

    public sealed class OutboxHandleTestsHandler : IOutboxHandler<RetryOutboxEvent>
    {
        private static readonly DateTimeOffset _nextScheduledAt = DateTimeOffset.UtcNow.AddHours(1);

        private static int _counter;

        public Task HandleAsync(IEnumerable<OutboxMessage<RetryOutboxEvent>> messages, CancellationToken cancellationToken)
        {
            if (_counter > 0)
                return Task.CompletedTask;

            _counter++;

            foreach (var message in messages)
                message.Retry(_nextScheduledAt);

            return Task.CompletedTask;
        }
    }

    public sealed record RetryOutboxEvent(int Id, string Name);
}
