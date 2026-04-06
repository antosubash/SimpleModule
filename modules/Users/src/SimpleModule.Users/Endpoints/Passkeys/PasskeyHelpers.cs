namespace SimpleModule.Users.Endpoints.Passkeys;

internal static class PasskeyHelpers
{
    internal static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
