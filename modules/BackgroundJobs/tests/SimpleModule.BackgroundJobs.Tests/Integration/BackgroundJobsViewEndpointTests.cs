using System.Net;
using FluentAssertions;
using SimpleModule.Tests.Shared.Fixtures;

namespace BackgroundJobs.Tests.Integration;

public class BackgroundJobsViewEndpointTests : IClassFixture<SimpleModuleWebApplicationFactory>
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public BackgroundJobsViewEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Dashboard_ReturnsHtmlPage()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/admin/jobs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
    }

    [Fact]
    public async Task List_ReturnsHtmlPage()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/admin/jobs/list");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
    }

    [Fact]
    public async Task Detail_NonExistentId_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/admin/jobs/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Recurring_ReturnsHtmlPage()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/admin/jobs/recurring");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
    }
}
