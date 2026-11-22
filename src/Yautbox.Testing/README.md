# Yautbox.Testing

Unit-testing helpers for Yautbox handlers.

An `IOutboxHandler<T>` receives `Yautbox.Handlers.OutboxMessage<T>`, whose constructor is internal: in production
only the outbox runner creates it. This package lets a unit test build such a message directly, without storage,
a host or a runner.

## What the package provides

- `OutboxMessage.Of<T>(payload, attempt = 0, createdAt = null)` in the `Yautbox.Testing` namespace returns a ready
  `OutboxMessage<T>` carrying the given payload:
    - `attempt` is the value of `OutboxMessage<T>.Attempt`, `0` by default (the first delivery);
    - `createdAt` is the value of `OutboxMessage<T>.CreatedAt`, the current UTC time by default;
    - `Retry(...)` can be called on the message and does not throw.

## Installation

Reference the package from the test project only:

```bash
dotnet add package Yautbox.Testing
```

It depends on `Yautbox` and follows the same version.

## Example

```csharp
using Yautbox.Testing;

public class OrderPlacedHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldPublishOrder()
    {
        // Arrange
        var publisher = Substitute.For<IOrderPublisher>();
        var handler = new OrderPlacedHandler(publisher);
        var message = OutboxMessage.Of(new OrderPlaced("A-123"), attempt: 2);

        // Act
        await handler.HandleAsync([message], CancellationToken.None);

        // Assert
        await publisher.Received(1).PublishAsync("A-123", Arg.Any<CancellationToken>());
    }
}
```

## Limitations

- Every message built by `Of` has its own isolated processing context. `Retry(...)` is accepted but its effect is
  not observable: the test cannot assert whether, or when, the handler asked for a retry.
- The message is detached from any storage, so it has no meaningful identifier and nothing is written to the outbox.
- To test the full pipeline (fetching, retries, visibility timeout, cleanup), use an integration test with a real
  host, for example with the `Yautbox.InMemory` provider.
