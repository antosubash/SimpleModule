using FluentAssertions;
using SimpleModule.Generator.Tests.Helpers;

namespace SimpleModule.Generator.Tests;

public class EventDiscoveryTests
{
    private const string ModuleWithEventsSource = """
        using System.Threading.Tasks;
        using SimpleModule.Core;
        using SimpleModule.Core.Events;

        namespace TestApp
        {
            [Module("Flags", RoutePrefix = "/api/flags")]
            public class FlagsModule : IModule { }

            public sealed record FlagToggled(string Name) : DomainEvent;

            public sealed record OtherThing(string Id) : DomainEvent;

            public class OtherThingHandler
            {
                public Task Handle(OtherThing evt) => Task.CompletedTask;
            }

            public class NotAnEventHandler
            {
                public Task Handle(string notAnEvent) => Task.CompletedTask;
            }

            public class IrrelevantlyNamedClass
            {
                public Task Handle(FlagToggled evt) => Task.CompletedTask;
            }
        }
        """;

    [Fact]
    public void Discovers_DomainEventTypes_AsPublished()
    {
        var compilation = GeneratorTestHelper.CreateCompilation(ModuleWithEventsSource);

        var data = SymbolDiscovery.Extract(compilation, CancellationToken.None);

        data.EventTypes.Should()
            .Contain(e =>
                e.FullyQualifiedName == "global::TestApp.FlagToggled" && e.ModuleName == "Flags"
            );
        data.EventTypes.Should()
            .Contain(e =>
                e.FullyQualifiedName == "global::TestApp.OtherThing" && e.ModuleName == "Flags"
            );
    }

    [Fact]
    public void Discovers_ConventionalHandlers_AsConsumed()
    {
        var compilation = GeneratorTestHelper.CreateCompilation(ModuleWithEventsSource);

        var data = SymbolDiscovery.Extract(compilation, CancellationToken.None);

        data.EventHandlers.Should()
            .ContainSingle(h =>
                h.EventFullyQualifiedName == "global::TestApp.OtherThing" && h.ModuleName == "Flags"
            );
    }

    [Fact]
    public void Ignores_HandlersWithNonEventFirstParameter_AndNonConventionalNames()
    {
        var compilation = GeneratorTestHelper.CreateCompilation(ModuleWithEventsSource);

        var data = SymbolDiscovery.Extract(compilation, CancellationToken.None);

        data.EventHandlers.Should()
            .NotContain(h => h.EventFullyQualifiedName == "global::TestApp.FlagToggled");
        data.EventHandlers.Should().HaveCount(1);
    }
}
