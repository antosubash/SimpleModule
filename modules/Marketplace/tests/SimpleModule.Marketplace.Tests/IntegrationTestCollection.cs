using SimpleModule.Tests.Shared.Fixtures;
using Xunit;

#pragma warning disable CA1711 // xUnit collection fixture naming convention
[CollectionDefinition("Integration")]
public sealed class IntegrationTestCollection
    : ICollectionFixture<SimpleModuleWebApplicationFactory>;
#pragma warning restore CA1711
