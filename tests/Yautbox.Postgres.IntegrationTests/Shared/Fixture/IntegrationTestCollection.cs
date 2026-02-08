using Xunit;

namespace Yautbox.Postgres.IntegrationTests.Shared.Fixture;

[CollectionDefinition(nameof(IntegrationTestCollection), DisableParallelization = true)]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>;
