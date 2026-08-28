namespace SimpleModule.Users.Services;

/// <summary>
/// Tunes the resend cooldown and verification attempt cap that protect
/// the email and phone confirmation flows.
/// </summary>
public sealed class VerificationThrottleOptions
{
    /// <summary>
    /// Minimum interval between resends for a single (user, channel) pair.
    /// </summary>
    public TimeSpan ResendCooldown { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Number of failed code submissions before the (user, channel) pair is
    /// locked out from further attempts.
    /// </summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>
    /// How long a (user, channel) pair stays locked out after exhausting
    /// <see cref="MaxFailedAttempts"/>.
    /// </summary>
    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);
}
