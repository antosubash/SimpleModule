using Microsoft.Extensions.Hosting;

namespace SimpleModule.Core.Hosting;

/// <summary>
/// Shared environment classification for the framework's fail-fast guards.
/// </summary>
public static class HostEnvironmentExtensions
{
    /// <summary>
    /// The well-known name for the test environment used by the in-process
    /// <c>WebApplicationFactory</c> harnesses.
    /// </summary>
    public const string TestingEnvironmentName = "Testing";

    /// <summary>
    /// True for the developer-machine and CI/test environments — Development and
    /// Testing — where compiled-in defaults (seed passwords, ephemeral signing
    /// keys, the ROPC password grant) are acceptable conveniences.
    /// <para>
    /// Every other environment (Staging, QA, Production, or any custom name) is
    /// treated as a real deployment: the security guards require explicit
    /// configuration and refuse the unsafe defaults. Using a single predicate
    /// keeps <c>UserSeedService</c> and <c>OpenIddictProductionGuard</c>
    /// consistent — a deployment is never hardened by one guard and waved
    /// through by the other.
    /// </para>
    /// </summary>
    public static bool IsLocalOrTest(this IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return environment.IsDevelopment() || environment.IsEnvironment(TestingEnvironmentName);
    }
}
