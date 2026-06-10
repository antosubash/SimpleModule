using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SimpleModule.Generator.Tests.Helpers;

/// <summary>
/// Exposes a fixed set of global analyzer-config options (build properties) to
/// generators under test, mimicking MSBuild's CompilerVisibleProperty plumbing.
/// </summary>
public sealed class TestAnalyzerConfigOptionsProvider(Dictionary<string, string> globalOptions)
    : AnalyzerConfigOptionsProvider
{
    public override AnalyzerConfigOptions GlobalOptions { get; } =
        new TestAnalyzerConfigOptions(globalOptions);

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) =>
        new TestAnalyzerConfigOptions([]);

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) =>
        new TestAnalyzerConfigOptions([]);

    private sealed class TestAnalyzerConfigOptions(Dictionary<string, string> options)
        : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value)
        {
            if (options.TryGetValue(key, out var found))
            {
                value = found;
                return true;
            }

            value = "";
            return false;
        }
    }
}
