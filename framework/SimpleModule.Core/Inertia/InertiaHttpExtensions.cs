using Microsoft.AspNetCore.Http;

namespace SimpleModule.Core.Inertia;

public static class InertiaHttpExtensions
{
    public const string InertiaHeader = "X-Inertia";
    public const string InertiaVersionHeader = "X-Inertia-Version";
    public const string InertiaLocationHeader = "X-Inertia-Location";

    public static bool IsInertia(this HttpRequest request) =>
        request.Headers.ContainsKey(InertiaHeader);
}
