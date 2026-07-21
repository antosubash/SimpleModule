using FluentAssertions;
using SimpleModule.Users.Services;

namespace SimpleModule.Users.Tests.Unit;

public class UserSeedServiceTests
{
    private const string Default = "Default123!";
    private const string Configured = "Configured123!";

    [Fact]
    public void ResolveSeedPassword_ConfiguredPassword_IsUsed()
    {
        UserSeedService.ResolveSeedPassword(Configured, Default).Should().Be(Configured);
    }

    /// <summary>
    /// Both seeded accounts fall back to their compiled-in defaults in every
    /// environment so the app is usable out of the box — the login page's
    /// quick-login buttons advertise these exact credentials.
    /// </summary>
    [Fact]
    public void ResolveSeedPassword_NoConfiguredPassword_FallsBackToDefault()
    {
        UserSeedService.ResolveSeedPassword(null, Default).Should().Be(Default);
    }

    [Fact]
    public void ResolveSeedPassword_EmptyConfiguredPassword_TreatedAsUnconfigured()
    {
        UserSeedService.ResolveSeedPassword("", Default).Should().Be(Default);
    }
}
