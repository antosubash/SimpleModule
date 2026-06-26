using Microsoft.AspNetCore.Http;

namespace SimpleModule.Core.Inertia;

/// <summary>
/// Implement and register (scoped) to contribute raw HTML into the document &lt;head&gt;
/// of every full server-rendered Inertia page. Output replaces the
/// <c>&lt;!--HEAD_CONTRIBUTIONS--&gt;</c> placeholder (just before <c>&lt;/head&gt;</c>) per
/// request. Contributors are responsible for their own escaping.
/// </summary>
public interface IInertiaHeadContributor
{
    ValueTask<string?> GetHeadHtmlAsync(HttpContext context);
}
