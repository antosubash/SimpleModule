using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SimpleModule.Core.Exceptions;
using SimpleModule.Tests.Shared.Fakes;
using SimpleModule.Tests.Shared.Fixtures;
using SimpleModule.Users;
using SimpleModule.Users.Contracts;
using SimpleModule.Users.Contracts.Events;

namespace Users.Tests.Integration;

[Collection(TestCollections.Integration)]
public sealed class UserServiceTests
{
    private readonly SimpleModuleWebApplicationFactory _factory;

    public UserServiceTests(SimpleModuleWebApplicationFactory factory)
    {
        _factory = factory;
        // Force the host to spin up so the in-memory SQLite schema exists.
        _ = _factory.CreateClient();
    }

    private (UserService sut, TestMessageBus bus, IServiceScope scope) CreateSut()
    {
        var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var bus = new TestMessageBus();
        var sut = new UserService(userManager, roleManager, bus, NullLogger<UserService>.Instance);
        return (sut, bus, scope);
    }

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    [Fact]
    public async Task CreateUserAsync_PersistsUser_HashesPassword_AndPublishesEvent()
    {
        var (sut, bus, scope) = CreateSut();
        using var _ = scope;

        var email = UniqueEmail();
        var dto = await sut.CreateUserAsync(
            new CreateUserRequest
            {
                Email = email,
                DisplayName = "Alice",
                Password = "TestPass1234!",
            }
        );

        dto.Email.Should().Be(email);
        dto.DisplayName.Should().Be("Alice");
        dto.Id.Value.Should().NotBeNullOrEmpty();

        // Verify the user actually persisted, not just returned from the call.
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var persisted = await userManager.FindByEmailAsync(email);
        persisted.Should().NotBeNull();
        persisted!.DisplayName.Should().Be("Alice");

        // Identity must hash the password — the stored hash should not equal
        // the plaintext, and CheckPasswordAsync should accept the original.
        persisted.PasswordHash.Should().NotBeNullOrEmpty().And.NotBe("TestPass1234!");
        (await userManager.CheckPasswordAsync(persisted, "TestPass1234!")).Should().BeTrue();

        // Event published with the new user's data.
        var evt = bus.PublishedEvents.OfType<UserCreatedEvent>().Single();
        evt.UserId.Should().Be(dto.Id);
        evt.Email.Should().Be(email);
        evt.DisplayName.Should().Be("Alice");
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateEmail_ThrowsValidationException_AndDoesNotPublishEvent()
    {
        var (sut, bus, scope) = CreateSut();
        using var _ = scope;
        var email = UniqueEmail();

        await sut.CreateUserAsync(
            new CreateUserRequest
            {
                Email = email,
                DisplayName = "First",
                Password = "TestPass1234!",
            }
        );
        bus.PublishedEvents.Clear();

        var act = () =>
            sut.CreateUserAsync(
                new CreateUserRequest
                {
                    Email = email,
                    DisplayName = "Second",
                    Password = "TestPass1234!",
                }
            );

        await act.Should().ThrowAsync<ValidationException>();
        bus.PublishedEvents.OfType<UserCreatedEvent>().Should().BeEmpty();
    }

    [Fact]
    public async Task CreateUserAsync_WeakPassword_ThrowsValidationException()
    {
        var (sut, _, scope) = CreateSut();
        using var _scope = scope;

        var act = () =>
            sut.CreateUserAsync(
                new CreateUserRequest
                {
                    Email = UniqueEmail(),
                    DisplayName = "Weak",
                    Password = "abc",
                }
            );

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsPersistedUser()
    {
        var (sut, _, scope) = CreateSut();
        using var _scope = scope;

        var created = await sut.CreateUserAsync(
            new CreateUserRequest
            {
                Email = UniqueEmail(),
                DisplayName = "Bob",
                Password = "TestPass1234!",
            }
        );

        var fetched = await sut.GetUserByIdAsync(created.Id);

        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(created.Id);
        fetched.DisplayName.Should().Be("Bob");
    }

    [Fact]
    public async Task GetUserByIdAsync_UnknownId_ReturnsNull()
    {
        var (sut, _, scope) = CreateSut();
        using var _scope = scope;

        var fetched = await sut.GetUserByIdAsync(UserId.From(Guid.NewGuid().ToString()));

        fetched.Should().BeNull();
    }

    [Fact]
    public async Task UpdateUserAsync_PersistsChanges_AndPublishesEvent()
    {
        var (sut, bus, scope) = CreateSut();
        using var _ = scope;
        var created = await sut.CreateUserAsync(
            new CreateUserRequest
            {
                Email = UniqueEmail(),
                DisplayName = "Original",
                Password = "TestPass1234!",
            }
        );
        bus.PublishedEvents.Clear();

        var newEmail = UniqueEmail();
        await sut.UpdateUserAsync(
            created.Id,
            new UpdateUserRequest { Email = newEmail, DisplayName = "Updated" }
        );

        // Re-fetch from the DB to confirm persistence (not just return value).
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var persisted = await userManager.FindByIdAsync(created.Id.Value);
        persisted.Should().NotBeNull();
        persisted!.Email.Should().Be(newEmail);
        persisted.UserName.Should().Be(newEmail);
        persisted.DisplayName.Should().Be("Updated");

        var evt = bus.PublishedEvents.OfType<UserUpdatedEvent>().Single();
        evt.UserId.Should().Be(created.Id);
        evt.Email.Should().Be(newEmail);
    }

    [Fact]
    public async Task UpdateUserAsync_UnknownUser_ThrowsNotFound()
    {
        var (sut, _, scope) = CreateSut();
        using var _scope = scope;

        var act = () =>
            sut.UpdateUserAsync(
                UserId.From(Guid.NewGuid().ToString()),
                new UpdateUserRequest { Email = UniqueEmail(), DisplayName = "x" }
            );

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteUserAsync_RemovesFromDatabase_AndPublishesEvent()
    {
        var (sut, bus, scope) = CreateSut();
        using var _ = scope;
        var created = await sut.CreateUserAsync(
            new CreateUserRequest
            {
                Email = UniqueEmail(),
                DisplayName = "ToDelete",
                Password = "TestPass1234!",
            }
        );
        bus.PublishedEvents.Clear();

        await sut.DeleteUserAsync(created.Id);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        (await userManager.FindByIdAsync(created.Id.Value)).Should().BeNull();

        var evt = bus.PublishedEvents.OfType<UserDeletedEvent>().Single();
        evt.UserId.Should().Be(created.Id);
    }

    [Fact]
    public async Task DeleteUserAsync_UnknownUser_ThrowsNotFound()
    {
        var (sut, _, scope) = CreateSut();
        using var _scope = scope;

        var act = () => sut.DeleteUserAsync(UserId.From(Guid.NewGuid().ToString()));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetRoleIdsByNamesAsync_ReturnsIdsForExistingRoles_AndOmitsUnknown()
    {
        var (sut, _, scope) = CreateSut();
        using var _scope = scope;
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        var roleA = $"role-a-{Guid.NewGuid():N}";
        var roleB = $"role-b-{Guid.NewGuid():N}";
        await roleManager.CreateAsync(new ApplicationRole { Name = roleA });
        await roleManager.CreateAsync(new ApplicationRole { Name = roleB });

        var result = await sut.GetRoleIdsByNamesAsync([roleA, roleB, "missing-role"]);

        result.Should().HaveCount(2);
        result.Keys.Should().BeEquivalentTo([roleA, roleB]);

        var roleARecord = await roleManager.FindByNameAsync(roleA);
        result[roleA].Should().Be(roleARecord!.Id);
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsAllPersistedUsers()
    {
        var (sut, _, scope) = CreateSut();
        using var _scope = scope;
        var marker = $"marker-{Guid.NewGuid():N}";

        await sut.CreateUserAsync(
            new CreateUserRequest
            {
                Email = $"a-{marker}@example.com",
                DisplayName = "A",
                Password = "TestPass1234!",
            }
        );
        await sut.CreateUserAsync(
            new CreateUserRequest
            {
                Email = $"b-{marker}@example.com",
                DisplayName = "B",
                Password = "TestPass1234!",
            }
        );

        var all = await sut.GetAllUsersAsync();

        all.Where(u => u.Email.Contains(marker, StringComparison.Ordinal))
            .Select(u => u.DisplayName)
            .Should()
            .BeEquivalentTo(["A", "B"]);
    }
}
