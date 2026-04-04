using SimpleModule.Tests.Shared.Fixtures;
using Xunit;

[CollectionDefinition(TestCollections.Integration)]
public sealed class IntegrationTestCollection
    : ICollectionFixture<SimpleModuleWebApplicationFactory>;
