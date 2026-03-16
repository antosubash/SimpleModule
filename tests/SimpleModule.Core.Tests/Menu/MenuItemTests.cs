using FluentAssertions;
using SimpleModule.Core.Menu;

namespace SimpleModule.Core.Tests.Menu;

public class MenuItemTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var item = new MenuItem { Label = "Test", Url = "/test" };

        item.Icon.Should().BeEmpty();
        item.Order.Should().Be(0);
        item.Section.Should().Be(MenuSection.Navbar);
        item.RequiresAuth.Should().BeTrue();
        item.Group.Should().BeNull();
    }

    [Fact]
    public void AllProperties_CanBeSet()
    {
        var item = new MenuItem
        {
            Label = "Admin",
            Url = "/admin",
            Icon = "<svg/>",
            Order = 42,
            Section = MenuSection.UserDropdown,
            RequiresAuth = false,
            Group = "management",
        };

        item.Label.Should().Be("Admin");
        item.Url.Should().Be("/admin");
        item.Icon.Should().Be("<svg/>");
        item.Order.Should().Be(42);
        item.Section.Should().Be(MenuSection.UserDropdown);
        item.RequiresAuth.Should().BeFalse();
        item.Group.Should().Be("management");
    }
}
