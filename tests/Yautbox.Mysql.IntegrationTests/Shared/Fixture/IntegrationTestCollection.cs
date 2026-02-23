using Xunit;

namespace Yautbox.Mysql.IntegrationTests.Shared.Fixture;

[CollectionDefinition(nameof(IntegrationTestCollection), DisableParallelization = true)]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>;
