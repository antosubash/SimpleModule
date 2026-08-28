using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;

namespace SimpleModule.Users.Services;

/// <summary>
/// FusionCache-backed implementation of <see cref="IVerificationThrottle"/>.
/// Keys are namespaced so a single cache can host every module's rate-limited
/// counter without collisions. Values are deliberately tiny (<see cref="long"/>
/// timestamps and <see cref="int"/> counters) so distributed backends stay
/// cheap.
/// </summary>
public sealed class VerificationThrottle : IVerificationThrottle
{
    private readonly IFusionCache _cache;
    private readonly TimeProvider _timeProvider;
    private readonly VerificationThrottleOptions _options;

    public VerificationThrottle(
        IFusionCache cache,
        TimeProvider timeProvider,
        IOptions<VerificationThrottleOptions> options
    )
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<ResendDecision> TryAcquireResendSlotAsync(
        string userKey,
        VerificationChannel channel,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userKey);

        var key = ResendKey(userKey, channel);
        var nowTicks = _timeProvider.GetUtcNow().UtcTicks;
        var existing = await _cache.TryGetAsync<long>(key, token: cancellationToken);
        if (existing.HasValue)
        {
            var nextAllowedTicks = existing.Value;
            if (nextAllowedTicks > nowTicks)
            {
                var retryAfter = TimeSpan.FromTicks(nextAllowedTicks - nowTicks);
                return new ResendDecision(false, retryAfter);
            }
        }

        var nextAllowed = nowTicks + _options.ResendCooldown.Ticks;
        await _cache.SetAsync(
            key,
            nextAllowed,
            options =>
            {
                options.Duration = _options.ResendCooldown;
            },
            token: cancellationToken
        );
        return new ResendDecision(true, null);
    }

    public async Task<VerificationAttemptDecision> RecordAttemptAsync(
        string userKey,
        VerificationChannel channel,
        bool succeeded,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userKey);

        var failKey = FailKey(userKey, channel);
        var lockKey = LockKey(userKey, channel);

        if (succeeded)
        {
            await _cache.RemoveAsync(failKey, token: cancellationToken);
            await _cache.RemoveAsync(lockKey, token: cancellationToken);
            return new VerificationAttemptDecision(false, 0);
        }

        var currentMaybe = await _cache.TryGetAsync<int>(failKey, token: cancellationToken);
        var current = currentMaybe.HasValue ? currentMaybe.Value : 0;
        var next = current + 1;

        if (next >= _options.MaxFailedAttempts)
        {
            await _cache.SetAsync(
                lockKey,
                true,
                options =>
                {
                    options.Duration = _options.LockoutDuration;
                },
                token: cancellationToken
            );
            await _cache.RemoveAsync(failKey, token: cancellationToken);
            return new VerificationAttemptDecision(true, next);
        }

        await _cache.SetAsync(
            failKey,
            next,
            options =>
            {
                options.Duration = _options.LockoutDuration;
            },
            token: cancellationToken
        );
        return new VerificationAttemptDecision(false, next);
    }

    public async Task<bool> IsLockedOutAsync(
        string userKey,
        VerificationChannel channel,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userKey);

        var existing = await _cache.TryGetAsync<bool>(
            LockKey(userKey, channel),
            token: cancellationToken
        );
        return existing.HasValue && existing.Value;
    }

    private static string ResendKey(string userKey, VerificationChannel channel) =>
        $"verify:resend:{channel}:{userKey}";

    private static string FailKey(string userKey, VerificationChannel channel) =>
        $"verify:fail:{channel}:{userKey}";

    private static string LockKey(string userKey, VerificationChannel channel) =>
        $"verify:lock:{channel}:{userKey}";
}
