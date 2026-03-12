using FluentAssertions;
using SimpleModule.Core;
using SimpleModule.Core.Menu;

namespace SimpleModule.Core.Tests.Menu;

public class IModuleConfigureMenuTests
{
    private sealed class EmptyModule : IModule { }

    private sealed class MenuModule : IModule
    {
        public void ConfigureMenu(IMenuBuilder menus)
        {
            menus
                .Add(
                    new MenuItem
                    {
                        Label = "Nav",
                        Url = "/nav",
                        Section = MenuSection.Navbar,
                        Order = 1,
                    }
                )
                .Add(
                    new MenuItem
                    {
                        Label = "Drop",
                        Url = "/drop",
                        Section = MenuSection.UserDropdown,
                        Order = 2,
                    }
                );
        }
    }

    [Fact]
    public void DefaultConfigureMenu_DoesNotThrow()
    {
        IModule module = new EmptyModule();
        var builder = new MenuBuilder();

        var act = () => module.ConfigureMenu(builder);

        act.Should().NotThrow();
    }

    [Fact]
    public void DefaultConfigureMenu_AddsNoItems()
    {
        IModule module = new EmptyModule();
        var builder = new MenuBuilder();

        module.ConfigureMenu(builder);

        builder.ToList().Should().BeEmpty();
    }

    [Fact]
    public void ConcreteModule_ConfigureMenu_AddsItems()
    {
        var module = new MenuModule();
        var builder = new MenuBuilder();

        module.ConfigureMenu(builder);

        var items = builder.ToList();
        items.Should().HaveCount(2);
        items[0].Label.Should().Be("Nav");
        items[1].Label.Should().Be("Drop");
    }

    [Fact]
    public void MultipleModules_ConfigureMenu_AccumulateItems()
    {
        IModule module1 = new MenuModule();
        IModule module2 = new MenuModule();
        var builder = new MenuBuilder();

        module1.ConfigureMenu(builder);
        module2.ConfigureMenu(builder);

        builder.ToList().Should().HaveCount(4);
    }
}
