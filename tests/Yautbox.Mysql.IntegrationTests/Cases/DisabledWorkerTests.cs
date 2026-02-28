using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using Yautbox.Handlers;
using Yautbox.Mysql.Infrastructure.Database;
using Yautbox.Mysql.IntegrationTests.DbHelper;
using Yautbox.Mysql.IntegrationTests.Shared.Fixture;
using Yautbox.Mysql.Options;
using Yautbox.Runner.Options;
using Yautbox.Services;

namespace Yautbox.Mysql.IntegrationTests.Cases;

[Collection(nameof(IntegrationTestCollection))]
public class DisabledWorkerTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;

    private readonly OutboxDbHelper _outboxDbHelper;

    public DisabledWorkerTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;

        var mapper = _fixture.Services.GetRequiredService<IOutboxConnectionFactory>();
        var options = _fixture.Services.GetRequiredService<IOptions<MysqlOutboxRepositoryOptions>>();

        _outboxDbHelper = new OutboxDbHelper(mapper, options);
    }

    [Fact]
    public async Task Handler_ShouldIgnore_WhenItIsDisabled()
    {
        // Arrange
        using var serviceScope = _fixture.Services.CreateScope();
        var outboxService = serviceScope.ServiceProvider.GetRequiredService<IOutboxService>();

        var events = Enumerable.Range(0, 100)
            .Select(i => new TestMessage(i, $"Name_{i}"))
            .ToArray();

        // Act
        await outboxService.HandleAsync(messages: events, cancellationToken: CancellationToken.None);

        // Assert
        await Task.Delay(TimeSpan.FromSeconds(5));

        var storedMessages = await _outboxDbHelper
            .GetAsync<TestMessage>()
            .ToArrayAsync();

        storedMessages.Should().HaveCount(events.Length).And.AllSatisfy(r => r.IsDeleted.Should().BeFalse());
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _outboxDbHelper.DisposeAsync().AsTask();

    public sealed record TestMessage(int Id, string Name);

    public sealed class TestMessageHandler : IOutboxHandler<TestMessage>
    {
        public Task HandleAsync(IEnumerable<Handlers.OutboxMessage<TestMessage>> messages, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    public sealed class TestMessageHandlerOptions : IOutboxRunnerOptions
    {
        public string? Identifier => null;
        public TimeSpan PollDelay => TimeSpan.FromSeconds(1);
        public int BufferSize => 100;
        public TimeSpan HandleTimeout => TimeSpan.FromMinutes(10);
        public bool IsEnabled => false;
        public TimeSpan Visibility => TimeSpan.FromMinutes(10);
        public TimeSpan? BackupInterval => null;
        public ExecutionPolicy ExecutionPolicy => ExecutionPolicy.Parallel;
        public DeletePolicy CancellationPolicy => DeletePolicy.Safe;
        public DeletePolicy DeletePolicy => DeletePolicy.Safe;
        public TimeSpan FailureDelay => TimeSpan.FromSeconds(5);
        public int WorkersCount => 4;
    }
}
