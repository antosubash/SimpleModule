using SimpleModule.Tests.Shared.Fixtures;
using Xunit;

/// <summary>
/// Shares a single <see cref="SimpleModuleWebApplicationFactory"/> across all test classes
/// in the same assembly that are decorated with <c>[Collection("Integration")]</c>.
/// This avoids spinning up a separate WebApplicationFactory per test class.
/// Copy this definition into each test assembly that uses the collection.
/// </summary>
[CollectionDefinition("Integration")]
public sealed class IntegrationTestCollection
    : ICollectionFixture<SimpleModuleWebApplicationFactory>;
