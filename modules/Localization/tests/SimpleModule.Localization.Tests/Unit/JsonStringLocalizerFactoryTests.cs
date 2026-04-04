using FluentAssertions;
using Microsoft.Extensions.Localization;
using SimpleModule.Localization.Services;

namespace SimpleModule.Localization.Tests.Unit;

public sealed class JsonStringLocalizerFactoryTests
{
    private readonly TranslationLoader _loader;
    private readonly JsonStringLocalizerFactory _factory;

    public JsonStringLocalizerFactoryTests()
    {
        _loader = new TranslationLoader();
        _loader.InitializeFromDictionary(
            "en",
            new Dictionary<string, string> { ["test.key"] = "Value" }
        );
        _factory = new JsonStringLocalizerFactory(_loader);
    }

    [Fact]
    public void Create_WithBaseNameAndLocation_ReturnsLocalizer()
    {
        var localizer = _factory.Create("test", "SomeAssembly");

        localizer.Should().NotBeNull();
        localizer.Should().BeOfType<JsonStringLocalizer>();
    }

    [Fact]
    public void Create_SameArguments_ReturnsCachedInstance()
    {
        var first = _factory.Create("test", "SomeAssembly");
        var second = _factory.Create("test", "SomeAssembly");

        first.Should().BeSameAs(second);
    }
}
