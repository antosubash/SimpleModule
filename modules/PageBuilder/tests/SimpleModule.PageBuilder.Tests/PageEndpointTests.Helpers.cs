using SimpleModule.Tests.Shared.Fixtures;

namespace PageBuilder.Tests;

[Collection(TestCollections.Integration)]
public partial class PageEndpointTests
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public PageEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
    }
}
