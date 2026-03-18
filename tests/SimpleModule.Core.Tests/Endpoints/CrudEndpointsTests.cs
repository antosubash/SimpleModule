using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using SimpleModule.Core.Endpoints;

namespace SimpleModule.Core.Tests.Endpoints;

public class CrudEndpointsTests
{
    [Fact]
    public async Task GetAll_ReturnsOkWithItems()
    {
        var items = new[] { "a", "b" };

        var result = await CrudEndpoints.GetAll(() => Task.FromResult<IEnumerable<string>>(items));

        var okResult = result.Should().BeOfType<Ok<IEnumerable<string>>>().Subject;
        okResult.Value.Should().HaveCount(2);
        okResult.Value.Should().ContainInOrder("a", "b");
    }

    [Fact]
    public async Task GetAll_WithEmptyList_ReturnsOkWithEmptyList()
    {
        var result = await CrudEndpoints.GetAll(() => Task.FromResult<IEnumerable<string>>([]));

        var okResult = result.Should().BeOfType<Ok<IEnumerable<string>>>().Subject;
        okResult.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var result = await CrudEndpoints.GetById(() => Task.FromResult<string?>("found-item"));

        var okResult = result.Should().BeOfType<Ok<string>>().Subject;
        okResult.Value.Should().Be("found-item");
    }

    [Fact]
    public async Task GetById_WhenNull_ReturnsNotFound()
    {
        var result = await CrudEndpoints.GetById(() => Task.FromResult<string?>(null));

        result.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task Create_ReturnsCreatedWithLocation()
    {
        var result = await CrudEndpoints.Create(
            () => Task.FromResult("new-item"),
            item => $"/items/{item}"
        );

        var createdResult = result.Should().BeOfType<Created<string>>().Subject;
        createdResult.Value.Should().Be("new-item");
        createdResult.Location.Should().Be("/items/new-item");
    }

    [Fact]
    public async Task Update_ReturnsOkWithEntity()
    {
        var result = await CrudEndpoints.Update(() => Task.FromResult("updated-item"));

        var okResult = result.Should().BeOfType<Ok<string>>().Subject;
        okResult.Value.Should().Be("updated-item");
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        var deleteCalled = false;

        var result = await CrudEndpoints.Delete(() =>
        {
            deleteCalled = true;
            return Task.CompletedTask;
        });

        result.Should().BeOfType<NoContent>();
        deleteCalled.Should().BeTrue();
    }
}
