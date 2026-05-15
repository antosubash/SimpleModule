namespace SimpleModule.Users.Services;

public enum VerificationChannel
{
    Email,
    Phone,
}

public sealed record ResendDecision(bool Allowed, TimeSpan? RetryAfter);

public sealed record VerificationAttemptDecision(bool LockedOut, int FailuresInWindow);

/// <summary>
/// Per-user, per-channel rate limit for verification-code resends and
/// per-user, per-channel attempt counter for failed code submissions. Both
/// counters are stored in the unified cache so they survive process recycles
/// when a distributed backend is wired in.
/// </summary>
public interface IVerificationThrottle
{
    /// <summary>
    /// Attempts to consume a resend slot. Returns <c>Allowed = true</c> when
    /// the cooldown is clear; otherwise <c>RetryAfter</c> is the time until
    /// the next resend is allowed.
    /// </summary>
    Task<ResendDecision> TryAcquireResendSlotAsync(
        string userKey,
        VerificationChannel channel,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Records the outcome of a code-submission attempt. On <c>succeeded = true</c>
    /// the failure counter is cleared. On a failure, the counter increments and
    /// the (user, channel) pair is locked out once it crosses
    /// <see cref="VerificationThrottleOptions.MaxFailedAttempts"/>.
    /// </summary>
    Task<VerificationAttemptDecision> RecordAttemptAsync(
        string userKey,
        VerificationChannel channel,
        bool succeeded,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// True when the (user, channel) pair is currently locked out from
    /// further attempts.
    /// </summary>
    Task<bool> IsLockedOutAsync(
        string userKey,
        VerificationChannel channel,
        CancellationToken cancellationToken = default
    );
}
