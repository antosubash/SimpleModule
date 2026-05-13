using Microsoft.AspNetCore.Http;

namespace SimpleModule.Core.Security;

public interface ISignedUrlGenerator
{
    string Sign(
        string path,
        IDictionary<string, string?>? query = null,
        DateTimeOffset? expiresAt = null,
        string? purpose = null
    );

    bool TryValidate(HttpRequest request, out SignedUrlClaims? claims);

    bool TryValidate(HttpRequest request, string? expectedPurpose, out SignedUrlClaims? claims);
}
