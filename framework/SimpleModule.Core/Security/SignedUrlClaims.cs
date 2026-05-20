namespace SimpleModule.Core.Security;

public sealed record SignedUrlClaims(string Path, string? Purpose, DateTimeOffset? ExpiresAt);
