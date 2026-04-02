using FluentAssertions;
using SimpleModule.Localization.Services;

namespace SimpleModule.Localization.Tests.Unit;

public sealed class TranslationLoaderTests
{
    private readonly TranslationLoader _loader = new();

    [Fact]
    public void GetTranslation_KnownKey_ReturnsValue()
    {
        _loader.InitializeFromDictionary("en", new Dictionary<string, string>
        {
            ["common.save"] = "Save",
        });

        var result = _loader.GetTranslation("common.save", "en");

        result.Should().Be("Save");
    }

    [Fact]
    public void GetTranslation_UnknownKey_ReturnsNull()
    {
        _loader.InitializeFromDictionary("en", new Dictionary<string, string>());

        var result = _loader.GetTranslation("unknown.key", "en");

        result.Should().BeNull();
    }

    [Fact]
    public void GetTranslation_FallsBackToEnglish()
    {
        _loader.InitializeFromDictionary("en", new Dictionary<string, string>
        {
            ["common.save"] = "Save",
        });

        var result = _loader.GetTranslation("common.save", "fr");

        result.Should().Be("Save");
    }

    [Fact]
    public void GetTranslation_LocalizedValue_ReturnsLocaleSpecific()
    {
        _loader.InitializeFromDictionary("en", new Dictionary<string, string>
        {
            ["common.save"] = "Save",
        });
        _loader.InitializeFromDictionary("es", new Dictionary<string, string>
        {
            ["common.save"] = "Guardar",
        });

        var result = _loader.GetTranslation("common.save", "es");

        result.Should().Be("Guardar");
    }

    [Fact]
    public void GetAllTranslations_ReturnsAllForLocale()
    {
        var translations = new Dictionary<string, string>
        {
            ["common.save"] = "Save",
            ["common.cancel"] = "Cancel",
        };
        _loader.InitializeFromDictionary("en", translations);

        var result = _loader.GetAllTranslations("en");

        result.Should().HaveCount(2);
        result["common.save"].Should().Be("Save");
    }

    [Fact]
    public void GetAllTranslations_UnknownLocale_ReturnsEmpty()
    {
        var result = _loader.GetAllTranslations("zz");

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetSupportedLocales_ReturnsOrderedList()
    {
        _loader.InitializeFromDictionary("es", new Dictionary<string, string>());
        _loader.InitializeFromDictionary("en", new Dictionary<string, string>());

        var result = _loader.GetSupportedLocales();

        result.Should().ContainInOrder("en", "es");
    }

    [Fact]
    public void FlattenJson_NestedObjects_ProducesDotNotation()
    {
        var json = """{"a":{"b":"value"}}""";

        var result = TranslationLoader.FlattenJson(json);

        result.Should().ContainKey("a.b");
        result["a.b"].Should().Be("value");
    }

    [Fact]
    public void LoadFromDirectory_LoadsJsonFiles()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Locales");
        var loader = new TranslationLoader();

        loader.LoadFromDirectory(dir);

        loader.GetTranslation("common.save", "en").Should().Be("Save");
        loader.GetTranslation("common.save", "es").Should().Be("Guardar");
    }
}
