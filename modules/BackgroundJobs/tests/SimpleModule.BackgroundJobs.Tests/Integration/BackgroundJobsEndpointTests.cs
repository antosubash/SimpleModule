using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SimpleModule.BackgroundJobs;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core;
using SimpleModule.Tests.Shared.Fixtures;

namespace BackgroundJobs.Tests.Integration;

[Collection("Integration")]
public class BackgroundJobsEndpointTests
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public BackgroundJobsEndpointTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // --- GET /api/jobs ---

    [Fact]
    public async Task GetAll_WithViewPermission_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient([BackgroundJobsPermissions.ViewJobs]);

        var response = await client.GetAsync("/api/jobs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAll_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/jobs");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_Returns_PagedResult()
    {
        var client = _factory.CreateAuthenticatedClient([BackgroundJobsPermissions.ViewJobs]);

        var response = await client.GetAsync("/api/jobs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<JobSummaryDto>>();
        result.Should().NotBeNull();
        result!.Page.Should().BeGreaterThan(0);
    }

    // --- GET /api/jobs/{id} ---

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient([BackgroundJobsPermissions.ViewJobs]);

        var response = await client.GetAsync($"/api/jobs/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/jobs/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- POST /api/jobs/{id}/cancel ---

    [Fact]
    public async Task Cancel_WithManagePermission_DoesNotReturn401()
    {
        var client = _factory.CreateAuthenticatedClient([BackgroundJobsPermissions.ManageJobs]);

        var response = await client.PostAsync($"/api/jobs/{Guid.NewGuid()}/cancel", null);

        // May fail with 404/500 since no real job exists, but should not be 401
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Cancel_WithViewOnlyPermission_Returns403()
    {
        var client = _factory.CreateAuthenticatedClient([BackgroundJobsPermissions.ViewJobs]);

        var response = await client.PostAsync($"/api/jobs/{Guid.NewGuid()}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // --- POST /api/jobs/{id}/retry ---

    [Fact]
    public async Task Retry_WithManagePermission_DoesNotReturn401()
    {
        var client = _factory.CreateAuthenticatedClient([BackgroundJobsPermissions.ManageJobs]);

        var response = await client.PostAsync($"/api/jobs/{Guid.NewGuid()}/retry", null);

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Retry_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"/api/jobs/{Guid.NewGuid()}/retry", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- GET /api/jobs/recurring ---

    [Fact]
    public async Task GetRecurring_WithViewPermission_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient([BackgroundJobsPermissions.ViewJobs]);

        var response = await client.GetAsync("/api/jobs/recurring");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRecurring_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/jobs/recurring");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- POST /api/jobs/recurring/{id}/toggle ---

    [Fact]
    public async Task ToggleRecurring_NonExistentId_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient([BackgroundJobsPermissions.ManageJobs]);

        var response = await client.PostAsync($"/api/jobs/recurring/{Guid.NewGuid()}/toggle", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ToggleRecurring_ViewOnly_Returns403()
    {
        var client = _factory.CreateAuthenticatedClient([BackgroundJobsPermissions.ViewJobs]);

        var response = await client.PostAsync($"/api/jobs/recurring/{Guid.NewGuid()}/toggle", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // --- DELETE /api/jobs/recurring/{id} ---

    [Fact]
    public async Task DeleteRecurring_WithManagePermission_DoesNotReturn401()
    {
        var client = _factory.CreateAuthenticatedClient([BackgroundJobsPermissions.ManageJobs]);

        var response = await client.DeleteAsync($"/api/jobs/recurring/{Guid.NewGuid()}");

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteRecurring_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"/api/jobs/recurring/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
