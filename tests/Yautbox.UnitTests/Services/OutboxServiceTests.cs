using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using Yautbox.Entities;
using Yautbox.Extensions.Outbox;
using Yautbox.Infrastructure.DateTime;
using Yautbox.Infrastructure.Waiter;
using Yautbox.Metrics;
using Yautbox.Provider;
using Yautbox.Provider.Contracts;
using Yautbox.Registy;
using Yautbox.Runner.Options;
using Yautbox.Services;
using Yautbox.Tracing;

namespace Yautbox.UnitTests.Services;

public class OutboxServiceTests
{
    [Fact]
    public async Task HandleAsync_ShouldHandleSuccess()
    {
        // Arrange
        var outboxMessages = new List<OutboxMessage<Message>>();

        var outboxProvider = Substitute.For<IOutboxProvider>();
        outboxProvider
            .AddAsync(Arg.Any<IReadOnlyCollection<AddRequest<Message>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<OutboxMessageId>>([OutboxMessageId.Empty]))
            .AndDoes(c => outboxMessages.AddRange(c.Arg<IReadOnlyCollection<AddRequest<Message>>>().Select(x => x.Message)));

        var now = DateTimeOffset.UtcNow;

        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider
            .GetNow()
            .Returns(now);

        var service = Create(outboxProvider, dateTimeProvider: dateTimeProvider);

        var message = new Message();

        // Act
        var outboxMessageId = await ((IOutboxService)service).HandleAsync(messages: [message]);

        // Assert
        outboxMessageId.Should().ContainSingle().Which.Should().Be(OutboxMessageId.Empty);

        var expected = new OutboxMessage<Message>(
            Id: OutboxMessageId.Empty,
            Payload: message,
            CreatedAt: now,
            Attempt: 0,
            ScheduledAt: null);

        outboxMessages
            .Should().ContainSingle()
            .Which.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task CancelAsync_ShouldCallCancel()
    {
        // Arrange
        var ids = new List<OutboxMessageId>();

        var outboxProvider = Substitute.For<IOutboxProvider>();
        outboxProvider
            .CancelAsync(
                Arg.Any<string>(),
                Arg.Any<IReadOnlyCollection<OutboxMessageId>>(),
                Arg.Any<DeletePolicy>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(c => ids.AddRange(c.Arg<IReadOnlyCollection<OutboxMessageId>>()));

        var service = Create(outboxProvider);

        // Act
        await service.CancelAsync<Message>(id: OutboxMessageId.Empty, CancellationToken.None);

        // Assert
        ids
            .Should().ContainSingle()
            .Which.Should().BeEquivalentTo(OutboxMessageId.Empty);
    }

    [Fact]
    public async Task PolymorphicHandleAsync_ShouldRouteMessagesByRuntimeTypeAndPreserveIdOrder()
    {
        // Arrange
        IReadOnlyCollection<AddRequest<object>>? capturedRequests = null;
        var provider = Substitute.For<IOutboxProvider>();
        provider
            .AddAsync(Arg.Any<IReadOnlyCollection<AddRequest<object>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<OutboxMessageId>>([new OutboxMessageId(11), new OutboxMessageId(12), new OutboxMessageId(13)]))
            .AndDoes(call => capturedRequests = call.Arg<IReadOnlyCollection<AddRequest<object>>>());

        var service = Create(provider);
        IPolymorphicOutboxService polymorphicService = service;
        object[] messages = [new FirstMessage(), new SecondMessage(), new FirstMessage()];

        // Act
        var ids = await polymorphicService.HandleAsync(messages);

        // Assert
        ids.Should().Equal(new OutboxMessageId(11), new OutboxMessageId(12), new OutboxMessageId(13));
        capturedRequests.Should().NotBeNull();
        capturedRequests!.Select(request => request.Identifier).Should().SatisfyRespectively(
            first => first.Should().Contain(typeof(FirstMessage).FullName!),
            second => second.Should().Contain(typeof(SecondMessage).FullName!),
            third => third.Should().Contain(typeof(FirstMessage).FullName!));
    }

    [Fact]
    public async Task HandleAsync_ShouldStoreAllMessagesUnderDeclaredType()
    {
        // Arrange
        IReadOnlyCollection<AddRequest<object>>? capturedRequests = null;
        var provider = Substitute.For<IOutboxProvider>();
        provider
            .AddAsync(Arg.Any<IReadOnlyCollection<AddRequest<object>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyCollection<OutboxMessageId>>([new OutboxMessageId(21), new OutboxMessageId(22)]))
            .AndDoes(call => capturedRequests = call.Arg<IReadOnlyCollection<AddRequest<object>>>());

        var service = Create(provider);
        object[] messages = [new FirstMessage(), new SecondMessage()];

        // Act
        var ids = await ((IOutboxService)service).HandleAsync(messages);

        // Assert
        ids.Should().Equal(new OutboxMessageId(21), new OutboxMessageId(22));
        capturedRequests.Should().NotBeNull();
        capturedRequests!.Should().OnlyContain(request =>
            request.Identifier.Contains(typeof(object).FullName!, StringComparison.Ordinal));
    }

    [Fact]
    public async Task PolymorphicHandleAsync_ShouldReturnEmptyForEmptyBatch()
    {
        var service = Create();
        IPolymorphicOutboxService polymorphicService = service;

        var ids = await polymorphicService.HandleAsync(Array.Empty<object>());

        ids.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ShouldTraceEnqueueFailure()
    {
        // Arrange
        var exception = new InvalidOperationException("Provider failed");
        var provider = Substitute.For<IOutboxProvider>();
        provider
            .AddAsync(Arg.Any<IReadOnlyCollection<AddRequest<Message>>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyCollection<OutboxMessageId>>(exception));

        var traceScope = Substitute.For<IOutboxTraceScope>();
        var tracer = Substitute.For<IOutboxTracer>();
        tracer.StartEnqueue(Arg.Any<string>(), 1).Returns(traceScope);
        var service = Create(provider, tracer: tracer);

        // Act
        Func<Task> action = () => ((IOutboxService)service).HandleAsync([new Message()]);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>();
        traceScope.Received(1).SetFailed(exception);
        traceScope.Received(1).Dispose();
    }

    private static OutboxService Create(
        IOutboxProvider? outboxProvider = null,
        IInfrastructureReadinessWaiter? waiter = null,
        IDateTimeProvider? dateTimeProvider = null,
        IOutboxTracer? tracer = null)
    {
        if (waiter is null)
        {
            waiter = Substitute.For<IInfrastructureReadinessWaiter>();

            waiter
                .WaitAsync(CancellationToken.None)
                .Returns(Task.CompletedTask);
        }

        var outboxRegistry =
            new OutboxRegistry(new OptionsManager<OutboxRegistryOptions>(new OptionsFactory<OutboxRegistryOptions>([], [])));

        if (tracer is null)
        {
            tracer = Substitute.For<IOutboxTracer>();
            tracer.StartEnqueue(Arg.Any<string>(), Arg.Any<int>()).Returns(Substitute.For<IOutboxTraceScope>());
        }

        return new OutboxService(
            outboxProvider ?? Substitute.For<IOutboxProvider>(),
            NullLogger<OutboxService>.Instance,
            waiter,
            dateTimeProvider ?? new DateTimeProvider(),
            outboxRegistry,
            new DefaultMetricsHandler(),
            tracer);
    }

    private sealed class Message;
    private sealed class FirstMessage;
    private sealed class SecondMessage;
}
