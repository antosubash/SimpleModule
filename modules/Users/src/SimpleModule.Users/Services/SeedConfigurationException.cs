namespace SimpleModule.Users.Services;

/// <summary>
/// Thrown when seeding cannot proceed safely — e.g. a required seed password is
/// missing outside Development. Unlike transient database errors (which the seed
/// service logs and tolerates), this exception is allowed to escape
/// <see cref="UserSeedService.StartAsync"/> so host startup fails loudly instead
/// of seeding a publicly known default credential.
/// </summary>
public sealed class SeedConfigurationException : InvalidOperationException
{
    public SeedConfigurationException() { }

    public SeedConfigurationException(string message)
        : base(message) { }

    public SeedConfigurationException(string message, Exception innerException)
        : base(message, innerException) { }
}
