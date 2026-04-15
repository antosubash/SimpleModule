namespace SimpleModule.Hosting;

/// <summary>
/// Configurable Content Security Policy directives. Modules that need external
/// resources (tile servers, CDNs) can append origins at startup via
/// <c>builder.AddSimpleModule(o => o.Csp.ConnectSources.Add("https://tiles.example.com"))</c>.
/// </summary>
public class CspOptions
{
    /// <summary>Extra origins appended to <c>connect-src</c>.</summary>
    public List<string> ConnectSources { get; } = [];

    /// <summary>Extra origins appended to <c>img-src</c>.</summary>
    public List<string> ImgSources { get; } = [];

    /// <summary>Extra origins appended to <c>worker-src</c>.</summary>
    public List<string> WorkerSources { get; } = [];

    /// <summary>Extra origins appended to <c>font-src</c>.</summary>
    public List<string> FontSources { get; } = [];

    /// <summary>Extra origins appended to <c>style-src</c>.</summary>
    public List<string> StyleSources { get; } = [];
}
