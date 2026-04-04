using SimpleModule.Tests.Shared.Fixtures;
using Xunit;

[CollectionDefinition("Integration")]
public sealed class IntegrationTestCollection
    : ICollectionFixture<SimpleModuleWebApplicationFactory>;
