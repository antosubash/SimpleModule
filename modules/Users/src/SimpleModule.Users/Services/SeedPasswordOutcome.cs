namespace SimpleModule.Users.Services;

/// <summary>
/// The decision made by <see cref="UserSeedService.ResolveSeedPassword"/> for a
/// single seed account.
/// </summary>
internal enum SeedPasswordOutcome
{
    /// <summary>Seed the account with the resolved password.</summary>
    Seed,

    /// <summary>Skip the (optional) account — unconfigured in a real deployment.</summary>
    Skip,

    /// <summary>
    /// Fail host startup — a required account is unconfigured in a real
    /// deployment and must not fall back to the compiled-in default.
    /// </summary>
    Fail,
}
