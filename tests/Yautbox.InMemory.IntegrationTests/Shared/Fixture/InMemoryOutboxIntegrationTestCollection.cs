using Xunit;

namespace Yautbox.InMemory.IntegrationTests.Shared.Fixture;

[CollectionDefinition(nameof(InMemoryOutboxIntegrationTestCollection))]
public sealed class InMemoryOutboxIntegrationTestCollection : ICollectionFixture<InMemoryOutboxIntegrationTestFixture>;
