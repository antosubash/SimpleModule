using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace SimpleModule.Core.Security;

public sealed class SignedUrlGenerator : ISignedUrlGenerator
{
    private const string SignatureQueryKey = "signature";
    private const string ExpiresQueryKey = "expires";
    private const string PurposeQueryKey = "purpose";
    private const string ProtectorPurpose = "SimpleModule.SignedUrl.v1";

    private readonly IDataProtector _protector;
    private readonly TimeProvider _clock;

    public SignedUrlGenerator(IDataProtectionProvider provider, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(clock);
        _protector = provider.CreateProtector(ProtectorPurpose);
        _clock = clock;
    }

    public string Sign(
        string path,
        IDictionary<string, string?>? query = null,
        DateTimeOffset? expiresAt = null,
        string? purpose = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var pairs = BuildPairs(query, expiresAt, purpose);
        var canonical = BuildCanonical(path, pairs);
        var signature = WebEncoders.Base64UrlEncode(
            _protector.Protect(Encoding.UTF8.GetBytes(canonical))
        );

        pairs.Add(new KeyValuePair<string, string?>(SignatureQueryKey, signature));
        return QueryHelpers.AddQueryString(path, pairs);
    }

    public bool TryValidate(HttpRequest request, out SignedUrlClaims? claims) =>
        TryValidate(request, expectedPurpose: null, out claims);

    public bool TryValidate(
        HttpRequest request,
        string? expectedPurpose,
        out SignedUrlClaims? claims
    )
    {
        claims = null;
        ArgumentNullException.ThrowIfNull(request);

        if (!request.Query.TryGetValue(SignatureQueryKey, out var providedSignature))
        {
            return false;
        }

        var pairs = new List<KeyValuePair<string, string?>>();
        string? purpose = null;
        DateTimeOffset? expiresAt = null;
        foreach (var (key, values) in request.Query)
        {
            if (string.Equals(key, SignatureQueryKey, StringComparison.Ordinal))
            {
                continue;
            }

            var value = (string?)values;
            pairs.Add(new KeyValuePair<string, string?>(key, value));

            if (string.Equals(key, PurposeQueryKey, StringComparison.Ordinal))
            {
                purpose = value;
            }
            else if (
                string.Equals(key, ExpiresQueryKey, StringComparison.Ordinal)
                && long.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var unix
                )
            )
            {
                expiresAt = DateTimeOffset.FromUnixTimeSeconds(unix);
            }
        }

        if (
            expectedPurpose is not null
            && !string.Equals(expectedPurpose, purpose, StringComparison.Ordinal)
        )
        {
            return false;
        }

        if (expiresAt is not null && _clock.GetUtcNow() > expiresAt.Value)
        {
            return false;
        }

        var path = request.Path.HasValue ? request.Path.Value! : "/";
        var canonical = BuildCanonical(path, pairs);
        var canonicalBytes = Encoding.UTF8.GetBytes(canonical);

        byte[] decrypted;
        try
        {
            var sigBytes = WebEncoders.Base64UrlDecode((string)providedSignature!);
            decrypted = _protector.Unprotect(sigBytes);
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }

        if (!CryptographicOperations.FixedTimeEquals(decrypted, canonicalBytes))
        {
            return false;
        }

        claims = new SignedUrlClaims(path, purpose, expiresAt);
        return true;
    }

    private static List<KeyValuePair<string, string?>> BuildPairs(
        IDictionary<string, string?>? query,
        DateTimeOffset? expiresAt,
        string? purpose
    )
    {
        var pairs = new List<KeyValuePair<string, string?>>();
        if (query is not null)
        {
            foreach (var (key, value) in query)
            {
                if (
                    string.Equals(key, SignatureQueryKey, StringComparison.Ordinal)
                    || string.Equals(key, ExpiresQueryKey, StringComparison.Ordinal)
                    || string.Equals(key, PurposeQueryKey, StringComparison.Ordinal)
                )
                {
                    throw new ArgumentException(
                        $"Query parameter '{key}' is reserved for signed URL metadata.",
                        nameof(query)
                    );
                }
                pairs.Add(new KeyValuePair<string, string?>(key, value));
            }
        }
        if (expiresAt is not null)
        {
            pairs.Add(
                new KeyValuePair<string, string?>(
                    ExpiresQueryKey,
                    expiresAt.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)
                )
            );
        }
        if (purpose is not null)
        {
            pairs.Add(new KeyValuePair<string, string?>(PurposeQueryKey, purpose));
        }
        return pairs;
    }

    private static string BuildCanonical(
        string path,
        IEnumerable<KeyValuePair<string, string?>> pairs
    )
    {
        var sorted = pairs
            .OrderBy(p => p.Key, StringComparer.Ordinal)
            .ThenBy(p => p.Value, StringComparer.Ordinal)
            .ToArray();

        var builder = new StringBuilder(path);
        if (sorted.Length == 0)
        {
            return builder.ToString();
        }
        builder.Append('?');
        for (var i = 0; i < sorted.Length; i++)
        {
            if (i > 0)
            {
                builder.Append('&');
            }
            builder.Append(Uri.EscapeDataString(sorted[i].Key));
            builder.Append('=');
            builder.Append(Uri.EscapeDataString(sorted[i].Value ?? string.Empty));
        }
        return builder.ToString();
    }
}
