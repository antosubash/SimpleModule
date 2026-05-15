using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Time.Testing;
using SimpleModule.Core.Security;

namespace SimpleModule.Core.Tests.Security;

public class SignedUrlGeneratorTests
{
    private static SignedUrlGenerator CreateGenerator(FakeTimeProvider? clock = null) =>
        new(new EphemeralDataProtectionProvider(), clock ?? new FakeTimeProvider());

    private static HttpRequest BuildRequest(string signedUrl)
    {
        var queryStart = signedUrl.IndexOf('?', StringComparison.Ordinal);
        var path = queryStart < 0 ? signedUrl : signedUrl[..queryStart];
        var queryString = queryStart < 0 ? string.Empty : signedUrl[queryStart..];

        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.QueryString = new QueryString(queryString);
        return context.Request;
    }

    [Fact]
    public void Sign_AppendsSignatureToQuery()
    {
        var generator = CreateGenerator();

        var url = generator.Sign("/files/123");

        url.Should().StartWith("/files/123?");
        url.Should().Contain("signature=");
    }

    [Fact]
    public void Sign_AndValidate_RoundTripsClaims()
    {
        var generator = CreateGenerator();
        var expires = DateTimeOffset.UtcNow.AddMinutes(10);

        var url = generator.Sign(
            "/files/abc",
            new Dictionary<string, string?> { ["userId"] = "42" },
            expires,
            purpose: "download"
        );

        var request = BuildRequest(url);
        var ok = generator.TryValidate(request, expectedPurpose: "download", out var claims);

        ok.Should().BeTrue();
        claims.Should().NotBeNull();
        claims!.Path.Should().Be("/files/abc");
        claims.Purpose.Should().Be("download");
        claims.ExpiresAt!.Value.ToUnixTimeSeconds().Should().Be(expires.ToUnixTimeSeconds());
    }

    [Fact]
    public void Validate_TamperedQuery_Fails()
    {
        var generator = CreateGenerator();
        var url = generator.Sign(
            "/files/abc",
            new Dictionary<string, string?> { ["userId"] = "42" }
        );
        var tampered = url.Replace("userId=42", "userId=43", StringComparison.Ordinal);

        var ok = generator.TryValidate(BuildRequest(tampered), out var claims);

        ok.Should().BeFalse();
        claims.Should().BeNull();
    }

    [Fact]
    public void Validate_TamperedSignature_Fails()
    {
        var generator = CreateGenerator();
        var url = generator.Sign("/files/abc");

        var sigIndex = url.IndexOf("signature=", StringComparison.Ordinal);
        var head = url[..(sigIndex + "signature=".Length)];
        var tampered = head + WebEncoders.Base64UrlEncode([0xAA, 0xBB, 0xCC, 0xDD]);

        var ok = generator.TryValidate(BuildRequest(tampered), out _);

        ok.Should().BeFalse();
    }

    [Fact]
    public void Validate_MissingSignature_Fails()
    {
        var generator = CreateGenerator();

        var ok = generator.TryValidate(BuildRequest("/files/abc?userId=42"), out _);

        ok.Should().BeFalse();
    }

    [Fact]
    public void Validate_ExpiredUrl_Fails()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var generator = CreateGenerator(clock);
        var url = generator.Sign("/files/abc", expiresAt: clock.GetUtcNow().AddMinutes(5));

        clock.Advance(TimeSpan.FromMinutes(6));

        var ok = generator.TryValidate(BuildRequest(url), out _);

        ok.Should().BeFalse();
    }

    [Fact]
    public void Validate_NoExpiry_AcceptedIndefinitely()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var generator = CreateGenerator(clock);
        var url = generator.Sign("/files/abc");

        clock.Advance(TimeSpan.FromDays(365));

        var ok = generator.TryValidate(BuildRequest(url), out _);

        ok.Should().BeTrue();
    }

    [Fact]
    public void Validate_PurposeMismatch_Fails()
    {
        var generator = CreateGenerator();
        var url = generator.Sign("/files/abc", purpose: "download");

        var ok = generator.TryValidate(BuildRequest(url), expectedPurpose: "unsubscribe", out _);

        ok.Should().BeFalse();
    }

    [Fact]
    public void Validate_PurposeMatch_Succeeds()
    {
        var generator = CreateGenerator();
        var url = generator.Sign("/files/abc", purpose: "download");

        var ok = generator.TryValidate(
            BuildRequest(url),
            expectedPurpose: "download",
            out var claims
        );

        ok.Should().BeTrue();
        claims!.Purpose.Should().Be("download");
    }

    [Fact]
    public void Validate_PurposeReuseAcrossEndpoints_Isolated()
    {
        var generator = CreateGenerator();
        var url = generator.Sign("/unsubscribe/123", purpose: "unsubscribe");

        var tamperedRequest = BuildRequest(
            url.Replace("/unsubscribe/123", "/delete-account/123", StringComparison.Ordinal)
        );

        var ok = generator.TryValidate(tamperedRequest, expectedPurpose: "unsubscribe", out _);

        ok.Should().BeFalse();
    }

    [Fact]
    public void Validate_QueryParameterOrder_DoesNotMatter()
    {
        var generator = CreateGenerator();
        var url = generator.Sign(
            "/files/abc",
            new Dictionary<string, string?>
            {
                ["a"] = "1",
                ["b"] = "2",
                ["c"] = "3",
            }
        );

        var path = url[..url.IndexOf('?', StringComparison.Ordinal)];
        var queryDict = QueryHelpers.ParseQuery(url[url.IndexOf('?', StringComparison.Ordinal)..]);
        var reordered = QueryHelpers.AddQueryString(
            path,
            queryDict.Reverse().ToDictionary(kv => kv.Key, kv => (string?)kv.Value.ToString())
        );

        var ok = generator.TryValidate(BuildRequest(reordered), out _);

        ok.Should().BeTrue();
    }

    [Fact]
    public void Sign_ReservedQueryKey_Throws()
    {
        var generator = CreateGenerator();

        var act = () =>
            generator.Sign("/files/abc", new Dictionary<string, string?> { ["signature"] = "x" });

        act.Should().Throw<ArgumentException>().WithMessage("*reserved*");
    }

    [Fact]
    public void Sign_PathContainingQueryString_Throws()
    {
        var generator = CreateGenerator();

        var act = () => generator.Sign("/files/abc?already=here");

        act.Should().Throw<ArgumentException>().WithMessage("*query string*");
    }

    [Fact]
    public void Validate_PurposeBoundUrl_WithoutExpectedPurpose_Fails()
    {
        var generator = CreateGenerator();
        var url = generator.Sign("/files/abc", purpose: "download");

        var ok = generator.TryValidate(BuildRequest(url), out _);

        ok.Should().BeFalse();
    }

    [Fact]
    public void Validate_NoPurposeUrl_WithExpectedPurpose_Fails()
    {
        var generator = CreateGenerator();
        var url = generator.Sign("/files/abc");

        var ok = generator.TryValidate(BuildRequest(url), expectedPurpose: "download", out _);

        ok.Should().BeFalse();
    }

    [Fact]
    public void Sign_DifferentKeyRings_ProduceDifferentSignatures()
    {
        var a = CreateGenerator();
        var b = CreateGenerator();

        var urlA = a.Sign("/files/abc");

        var ok = b.TryValidate(BuildRequest(urlA), out _);

        ok.Should().BeFalse();
    }
}
