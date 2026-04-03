using FluentAssertions;
using SimpleModule.Email.Services;

namespace SimpleModule.Email.Tests.Unit;

public sealed class EmailTemplateRendererTests
{
    [Fact]
    public void ExtractVariables_FindsAllVariables()
    {
        var template = "Hello {{name}}, welcome to {{app}}!";
        var vars = EmailTemplateRenderer.ExtractVariables(template);
        vars.Should().BeEquivalentTo(["name", "app"]);
    }

    [Fact]
    public void ExtractVariables_WithNoVariables_ReturnsEmpty()
    {
        var vars = EmailTemplateRenderer.ExtractVariables("No variables here.");
        vars.Should().BeEmpty();
    }

    [Fact]
    public void Render_WithHtmlTrue_EscapesValues()
    {
        var template = "Hello {{name}}";
        var vars = new Dictionary<string, string> { ["name"] = "<script>alert('xss')</script>" };
        var result = EmailTemplateRenderer.Render(template, vars, isHtml: true);
        result.Should().Be("Hello &lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;");
    }

    [Fact]
    public void Render_WithHtmlFalse_DoesNotEscape()
    {
        var template = "Hello {{name}}";
        var vars = new Dictionary<string, string> { ["name"] = "<b>John</b>" };
        var result = EmailTemplateRenderer.Render(template, vars, isHtml: false);
        result.Should().Be("Hello <b>John</b>");
    }

    [Fact]
    public void Render_WithMissingVariable_LeavesPlaceholder()
    {
        var template = "Hello {{name}}, your code is {{code}}";
        var vars = new Dictionary<string, string> { ["name"] = "John" };
        var result = EmailTemplateRenderer.Render(template, vars, isHtml: false);
        result.Should().Be("Hello John, your code is {{code}}");
    }
}
