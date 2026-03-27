namespace SimpleModule.Core.Authorization;

/// <summary>
/// Matches permission claims against requirements, supporting wildcard patterns.
/// </summary>
/// <remarks>
/// Wildcard rules:
/// <list type="bullet">
/// <item>"Products.*" matches "Products.View", "Products.Create", etc.</item>
/// <item>"*" matches any single-segment permission</item>
/// <item>Exact matches are checked first for performance</item>
/// <item>Only trailing wildcards are supported (no "*.View" or "Pro*.Create")</item>
/// </list>
/// </remarks>
public static class PermissionMatcher
{
    /// <summary>
    /// Returns true if the <paramref name="claim"/> satisfies the <paramref name="requirement"/>.
    /// </summary>
    public static bool IsMatch(string claim, string requirement)
    {
        // Exact match (fast path)
        if (string.Equals(claim, requirement, StringComparison.Ordinal))
            return true;

        // Wildcard: claim ends with ".*"
        if (claim.Length > 2
            && claim[^1] == '*'
            && claim[^2] == '.'
            && requirement.StartsWith(claim[..^1], StringComparison.Ordinal))
        {
            return true;
        }

        // Global wildcard: claim is just "*"
        if (claim is "*")
            return true;

        return false;
    }
}
