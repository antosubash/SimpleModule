using FluentAssertions;
using SimpleModule.Core.Modules;

namespace SimpleModule.Core.Tests.Modules;

public class ModuleManifestTests
{
    private const string SchemaV1Json = """
        {
          "schemaVersion": 1,
          "id": "SimpleModule.X",
          "name": "X",
          "displayName": "X Module",
          "version": "1.0.0",
          "frameworkCompat": ">=0.0.38 <1.0.0",
          "routePrefix": "/api/x",
          "viewPrefix": "/x",
          "schema": "X",
          "permissions": ["X.View", "X.Manage"],
          "frontendEntry": "_content/SimpleModule.X/SimpleModule.X.pages.js",
          "pages": ["X/Browse"],
          "eventsPublished": ["SimpleModule.X.Contracts.ThingHappened"],
          "eventsConsumed": ["SimpleModule.Y.Contracts.OtherThing"],
          "hasDbContext": true
        }
        """;

    [Fact]
    public void Parse_RoundtripsSchemaV1Json()
    {
        var manifest = ModuleManifestReader.Parse(SchemaV1Json);

        manifest.SchemaVersion.Should().Be(1);
        manifest.Id.Should().Be("SimpleModule.X");
        manifest.Name.Should().Be("X");
        manifest.DisplayName.Should().Be("X Module");
        manifest.Version.Should().Be("1.0.0");
        manifest.FrameworkCompat.Should().Be(">=0.0.38 <1.0.0");
        manifest.RoutePrefix.Should().Be("/api/x");
        manifest.ViewPrefix.Should().Be("/x");
        manifest.Schema.Should().Be("X");
        manifest.Permissions.Should().Equal("X.View", "X.Manage");
        manifest.FrontendEntry.Should().Be("_content/SimpleModule.X/SimpleModule.X.pages.js");
        manifest.Pages.Should().Equal("X/Browse");
        manifest.EventsPublished.Should().Equal("SimpleModule.X.Contracts.ThingHappened");
        manifest.EventsConsumed.Should().Equal("SimpleModule.Y.Contracts.OtherThing");
        manifest.HasDbContext.Should().BeTrue();
    }

    [Fact]
    public void Parse_ToleratesUnknownFieldsForForwardCompat()
    {
        var manifest = ModuleManifestReader.Parse(
            """{"schemaVersion":1,"id":"A","name":"A","someFutureField":{"x":1}}"""
        );

        manifest.Id.Should().Be("A");
    }

    [Fact]
    public void Parse_AllowsMissingOptionalFields()
    {
        var manifest = ModuleManifestReader.Parse("""{"schemaVersion":1,"id":"A","name":"A"}""");

        manifest.FrontendEntry.Should().BeNull();
        manifest.Permissions.Should().BeEmpty();
        manifest.Pages.Should().BeEmpty();
        manifest.EventsPublished.Should().BeEmpty();
        manifest.EventsConsumed.Should().BeEmpty();
        manifest.HasDbContext.Should().BeFalse();
    }

    [Fact]
    public void Parse_ThrowsOnInvalidJson()
    {
        var act = () => ModuleManifestReader.Parse("not json");

        act.Should().Throw<ModuleManifestException>();
    }

    [Fact]
    public void Parse_ThrowsOnNewerSchemaVersion()
    {
        var act = () => ModuleManifestReader.Parse("""{"schemaVersion":999,"id":"A","name":"A"}""");

        act.Should().Throw<ModuleManifestException>().WithMessage("*schemaVersion*999*");
    }

    [Fact]
    public void TryRead_ReturnsNullForAssemblyWithoutManifest()
    {
        var manifest = ModuleManifestReader.TryRead(typeof(object).Assembly);

        manifest.Should().BeNull();
    }

    [Fact]
    public void ModuleManifestAttribute_ExposesJson()
    {
        var attribute = new ModuleManifestAttribute("{}");

        attribute.Json.Should().Be("{}");
    }
}
