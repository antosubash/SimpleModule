using System.Security.Cryptography;

namespace SimpleModule.Core.Security;

public sealed class CspNonce : ICspNonce
{
    public string Value { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
}
