using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.Authorization;
using SimpleModule.FileStorage.Contracts;
using SimpleModule.Tests.Shared.Fixtures;
using Xunit;

namespace SimpleModule.FileStorage.Tests.Integration;

/// <summary>
/// Exercises <see cref="FileStoragePolicy"/> through the real GetById endpoint:
/// owner/admin succeed, a non-owner holding the FileStorage.View permission gets 404
/// (DenyAsNotFound — no existence leak).
/// </summary>
[Collection(TestCollections.Integration)]
public sealed class FileAccessPolicyTests(SimpleModuleWebApplicationFactory factory)
{
    private async Task<FileStorageId> SeedFileAsync(string ownerId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FileStorageDbContext>();
        var file = new StoredFile
        {
            FileName = "secret.txt",
            StoragePath = $"{ownerId}/secret.txt",
            ContentType = "text/plain",
            Size = 10,
            CreatedByUserId = ownerId,
        };
        db.StoredFiles.Add(file);
        await db.SaveChangesAsync(); // Id is assigned by the DB (int identity)
        return file.Id;
    }

    private HttpClient ClientFor(string userId, params string[] permissions) =>
        factory.CreateAuthenticatedClient(
            permissions,
            new Claim(ClaimTypes.NameIdentifier, userId)
        );

    [Fact]
    public async Task GetById_Owner_ReturnsOk()
    {
        var id = await SeedFileAsync("file-owner-1");
        var client = ClientFor("file-owner-1", FileStoragePermissions.View);

        var response = await client.GetAsync($"/api/files/{id.Value}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_NonOwnerWithPermission_Returns404()
    {
        var id = await SeedFileAsync("file-owner-2");
        var client = ClientFor("an-intruder", FileStoragePermissions.View);

        var response = await client.GetAsync($"/api/files/{id.Value}");

        // DenyAsNotFound: a permitted non-owner cannot tell the file exists.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_Admin_CanViewAnyFile()
    {
        var id = await SeedFileAsync("file-owner-3");
        var admin = factory.CreateAuthenticatedClient(
            [FileStoragePermissions.View],
            new Claim(ClaimTypes.Role, WellKnownRoles.Admin),
            new Claim(ClaimTypes.NameIdentifier, "file-admin")
        );

        var response = await admin.GetAsync($"/api/files/{id.Value}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        var client = ClientFor("file-owner-4", FileStoragePermissions.View);

        var response = await client.GetAsync("/api/files/2147483600");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
