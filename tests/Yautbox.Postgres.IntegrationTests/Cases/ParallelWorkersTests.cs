using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoBogus;
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
using Yautbox.Runner.Options;
using Yautbox.Services;

namespace Yautbox.Postgres.IntegrationTests.Cases;

[Collection(nameof(IntegrationTestCollection))]
public class ParallelWorkersTests
{
    private readonly IntegrationTestFixture _fixture;

    private readonly OutboxDbHelper _dbHelper;

    public ParallelWorkersTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;

        var mapper = _fixture.Services.GetRequiredService<IOutboxConnectionFactory>();
        var options = _fixture.Services.GetRequiredService<IOptions<PostgresOutboxRepositoryOptions>>();

        _dbHelper = new OutboxDbHelper(mapper, options);
    }

    [Fact]
    public async Task Handle_ShouldSendMessageAndHandleAsync()
    {
        // Arrange
        var messages = AutoFaker.Generate<ParallelWorkerEvent>(20);

        using var serviceScope = _fixture.Services.CreateScope();

        var outboxService = serviceScope.ServiceProvider.GetRequiredService<IOutboxService>();

        // Act
        await outboxService.HandleAsync(
            messages: messages,
            cancellationToken: CancellationToken.None);

        // Assert
        var counter = await Polly.Policy
            .HandleResult<IEnumerable<OutboxDbHelper.TableRow>>(i => i.Any(c => !c.IsDeleted))
            .WaitAndRetryAsync(3, a => TimeSpan.FromSeconds(Math.Min(10, a * a)))
            .ExecuteAsync(async () => await _dbHelper.GetAsync<ParallelWorkerEvent>().ToArrayAsync());

        counter
            .Should().HaveCount(messages.Count);
    }

    public sealed class OutboxHandleTestsHandler : IOutboxHandler<ParallelWorkerEvent>
    {
        public Task HandleAsync(IEnumerable<OutboxMessage<ParallelWorkerEvent>> messages, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    public sealed record ParallelWorkerEvent(int Id, string Name);

    public sealed class OutboxHandleTestsHandlerOptions : IOutboxRunnerOptions
    {
        public TimeSpan Visibility => TimeSpan.FromMinutes(10);
        public TimeSpan? BackupInterval => null;
        public ExecutionPolicy ExecutionPolicy => ExecutionPolicy.Parallel;
        public DeletePolicy CancellationPolicy => DeletePolicy.Safe;
        public DeletePolicy DeletePolicy => DeletePolicy.Safe;
        public TimeSpan FailureDelay => TimeSpan.FromSeconds(5);
        public string? Identifier => null;
        public TimeSpan PollDelay => TimeSpan.FromSeconds(5);
        public int BufferSize => 3;
        public TimeSpan HandleTimeout => TimeSpan.FromMinutes(10);
        public bool IsEnabled => true;
        public int WorkersCount => 5;
    }
}
