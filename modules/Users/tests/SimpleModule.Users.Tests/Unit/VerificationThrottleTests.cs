using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using SimpleModule.Users.Services;
using ZiggyCreatures.Caching.Fusion;

namespace SimpleModule.Users.Tests.Unit;

public class VerificationThrottleTests
{
    private static (VerificationThrottle Throttle, FakeTimeProvider Time, FusionCache Cache) CreateSut(
        VerificationThrottleOptions? options = null
    )
    {
        var cache = new FusionCache(new FusionCacheOptions { CacheName = "verify-tests" });
        var time = new FakeTimeProvider(startDateTime: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var opts = Options.Create(options ?? new VerificationThrottleOptions());
        return (new VerificationThrottle(cache, time, opts), time, cache);
    }

    [Fact]
    public async Task TryAcquire_first_request_is_allowed()
    {
        var (throttle, _, _) = CreateSut();

        var result = await throttle.TryAcquireResendSlotAsync("user-1", VerificationChannel.Email);

        result.Allowed.Should().BeTrue();
        result.RetryAfter.Should().BeNull();
    }

    [Fact]
    public async Task TryAcquire_within_cooldown_is_rejected_with_retry_after()
    {
        var (throttle, time, _) = CreateSut(
            new VerificationThrottleOptions { ResendCooldown = TimeSpan.FromSeconds(60) }
        );

        var first = await throttle.TryAcquireResendSlotAsync("user-1", VerificationChannel.Email);
        time.Advance(TimeSpan.FromSeconds(10));
        var second = await throttle.TryAcquireResendSlotAsync("user-1", VerificationChannel.Email);

        first.Allowed.Should().BeTrue();
        second.Allowed.Should().BeFalse();
        second.RetryAfter.Should().BeCloseTo(TimeSpan.FromSeconds(50), precision: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task TryAcquire_after_cooldown_is_allowed_again()
    {
        var (throttle, time, _) = CreateSut(
            new VerificationThrottleOptions { ResendCooldown = TimeSpan.FromSeconds(30) }
        );

        await throttle.TryAcquireResendSlotAsync("user-1", VerificationChannel.Email);
        time.Advance(TimeSpan.FromSeconds(31));
        var second = await throttle.TryAcquireResendSlotAsync("user-1", VerificationChannel.Email);

        second.Allowed.Should().BeTrue();
    }

    [Fact]
    public async Task TryAcquire_is_per_channel()
    {
        var (throttle, _, _) = CreateSut(
            new VerificationThrottleOptions { ResendCooldown = TimeSpan.FromMinutes(5) }
        );

        var email = await throttle.TryAcquireResendSlotAsync("user-1", VerificationChannel.Email);
        var phone = await throttle.TryAcquireResendSlotAsync("user-1", VerificationChannel.Phone);

        email.Allowed.Should().BeTrue();
        phone.Allowed.Should().BeTrue();
    }

    [Fact]
    public async Task TryAcquire_is_per_user()
    {
        var (throttle, _, _) = CreateSut(
            new VerificationThrottleOptions { ResendCooldown = TimeSpan.FromMinutes(5) }
        );

        var a = await throttle.TryAcquireResendSlotAsync("user-A", VerificationChannel.Email);
        var b = await throttle.TryAcquireResendSlotAsync("user-B", VerificationChannel.Email);

        a.Allowed.Should().BeTrue();
        b.Allowed.Should().BeTrue();
    }

    [Fact]
    public async Task RecordAttempt_locks_out_after_max_failures()
    {
        var (throttle, _, _) = CreateSut(
            new VerificationThrottleOptions { MaxFailedAttempts = 3 }
        );

        var a1 = await throttle.RecordAttemptAsync("user-1", VerificationChannel.Phone, succeeded: false);
        var a2 = await throttle.RecordAttemptAsync("user-1", VerificationChannel.Phone, succeeded: false);
        var a3 = await throttle.RecordAttemptAsync("user-1", VerificationChannel.Phone, succeeded: false);

        a1.LockedOut.Should().BeFalse();
        a2.LockedOut.Should().BeFalse();
        a3.LockedOut.Should().BeTrue();
        (await throttle.IsLockedOutAsync("user-1", VerificationChannel.Phone)).Should().BeTrue();
    }

    [Fact]
    public async Task RecordAttempt_success_clears_lockout()
    {
        var (throttle, _, _) = CreateSut(
            new VerificationThrottleOptions { MaxFailedAttempts = 2 }
        );

        await throttle.RecordAttemptAsync("user-1", VerificationChannel.Phone, succeeded: false);
        await throttle.RecordAttemptAsync("user-1", VerificationChannel.Phone, succeeded: false);
        (await throttle.IsLockedOutAsync("user-1", VerificationChannel.Phone)).Should().BeTrue();

        await throttle.RecordAttemptAsync("user-1", VerificationChannel.Phone, succeeded: true);

        (await throttle.IsLockedOutAsync("user-1", VerificationChannel.Phone)).Should().BeFalse();
    }

    [Fact]
    public async Task IsLockedOut_false_when_no_attempts_recorded()
    {
        var (throttle, _, _) = CreateSut();

        (await throttle.IsLockedOutAsync("nobody", VerificationChannel.Email)).Should().BeFalse();
    }
}
