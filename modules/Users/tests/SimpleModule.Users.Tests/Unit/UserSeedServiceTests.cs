using FluentAssertions;
using SimpleModule.Users.Services;

namespace SimpleModule.Users.Tests.Unit;

public class UserSeedServiceTests
{
    private const string Default = "Default123!";
    private const string Configured = "Configured123!";

    [Theory]
    [InlineData(true, true)] // local, required (admin)
    [InlineData(true, false)] // local, optional (demo)
    [InlineData(false, true)] // real deployment, required
    [InlineData(false, false)] // real deployment, optional
    public void ResolveSeedPassword_ConfiguredPassword_AlwaysUsed(
        bool isLocalOrTest,
        bool requiredOutsideLocal
    )
    {
        var outcome = UserSeedService.ResolveSeedPassword(
            Configured,
            Default,
            isLocalOrTest,
            requiredOutsideLocal,
            out var password
        );

        outcome.Should().Be(SeedPasswordOutcome.Seed);
        password.Should().Be(Configured);
    }

    [Theory]
    [InlineData(true)] // required (admin)
    [InlineData(false)] // optional (demo)
    public void ResolveSeedPassword_LocalOrTest_NoConfig_UsesDefault(bool requiredOutsideLocal)
    {
        var outcome = UserSeedService.ResolveSeedPassword(
            configuredPassword: null,
            Default,
            isLocalOrTest: true,
            requiredOutsideLocal,
            out var password
        );

        outcome.Should().Be(SeedPasswordOutcome.Seed);
        password.Should().Be(Default);
    }

    [Fact]
    public void ResolveSeedPassword_RealDeployment_RequiredAndUnconfigured_Fails()
    {
        // The security-critical path: the admin account must never fall back to
        // the compiled-in default outside a local/test environment.
        var outcome = UserSeedService.ResolveSeedPassword(
            configuredPassword: null,
            Default,
            isLocalOrTest: false,
            requiredOutsideLocal: true,
            out var password
        );

        outcome.Should().Be(SeedPasswordOutcome.Fail);
        password.Should().BeNull();
    }

    [Fact]
    public void ResolveSeedPassword_RealDeployment_OptionalAndUnconfigured_Skips()
    {
        var outcome = UserSeedService.ResolveSeedPassword(
            configuredPassword: null,
            Default,
            isLocalOrTest: false,
            requiredOutsideLocal: false,
            out var password
        );

        outcome.Should().Be(SeedPasswordOutcome.Skip);
        password.Should().BeNull();
    }

    [Fact]
    public void ResolveSeedPassword_EmptyConfiguredPassword_TreatedAsUnconfigured()
    {
        var outcome = UserSeedService.ResolveSeedPassword(
            configuredPassword: "",
            Default,
            isLocalOrTest: false,
            requiredOutsideLocal: true,
            out _
        );

        outcome.Should().Be(SeedPasswordOutcome.Fail);
    }
}
