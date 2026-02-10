using Xunit;

namespace Yautbox.Mssql.IntegrationTests.Shared.Fixture;

[CollectionDefinition(nameof(IntegrationTestCollection), DisableParallelization = true)]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>;
