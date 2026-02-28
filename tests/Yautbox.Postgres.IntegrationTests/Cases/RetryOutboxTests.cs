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
using Yautbox.Postgres.Infrastructure.Database;
using Yautbox.Postgres.IntegrationTests.DbHelper;
using Yautbox.Postgres.IntegrationTests.Shared.Fixture;
using Yautbox.Postgres.Options;
using Yautbox.Services;
using Microsoft.Extensions.Logging;
namespace Yautbox.Postgres.IntegrationTests.Cases;

[Collection(nameof(IntegrationTestCollection))]
public class RetryOutboxTests
{
    private readonly IntegrationTestFixture _fixture;

    private readonly OutboxDbHelper _dbHelper;

    public RetryOutboxTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;

        var mapper = _fixture.Services.GetRequiredService<IOutboxConnectionFactory>();
        var options = _fixture.Services.GetRequiredService<IOptions<PostgresOutboxRepositoryOptions>>();

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
        private readonly ILogger<OutboxHandleTestsHandler> _logger;
        public OutboxHandleTestsHandler(ILogger<OutboxHandleTestsHandler> logger)
        {
            _logger = logger;
        }

        private static readonly DateTimeOffset _nextScheduledAt = DateTimeOffset.UtcNow.AddHours(1);

        private static int _counter;

        public Task HandleAsync(IEnumerable<OutboxMessage<RetryOutboxEvent>> messages, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling messages.");
            if (_counter > 0)
            {
                _logger.LogInformation("Invariant hit: if condition evaluated true.");
                return Task.CompletedTask;
            }

            _counter++;

            foreach (var message in messages)
                message.Retry(_nextScheduledAt);

            return Task.CompletedTask;
        }
    }

    public sealed record RetryOutboxEvent(int Id, string Name);
}
