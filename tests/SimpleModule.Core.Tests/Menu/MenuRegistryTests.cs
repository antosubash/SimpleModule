using FluentAssertions;
using SimpleModule.Core.Menu;

namespace SimpleModule.Core.Tests.Menu;

public class MenuRegistryTests
{
    [Fact]
    public void GetItems_ReturnsOnlyItemsForRequestedSection()
    {
        var items = new List<MenuItem>
        {
            new()
            {
                Label = "Nav1",
                Url = "/nav1",
                Section = MenuSection.Navbar,
            },
            new()
            {
                Label = "Drop1",
                Url = "/drop1",
                Section = MenuSection.UserDropdown,
            },
            new()
            {
                Label = "Nav2",
                Url = "/nav2",
                Section = MenuSection.Navbar,
            },
        };
        var registry = new MenuRegistry(items);

        var navItems = registry.GetItems(MenuSection.Navbar);
        var dropItems = registry.GetItems(MenuSection.UserDropdown);

        navItems.Should().HaveCount(2);
        navItems.Select(i => i.Label).Should().BeEquivalentTo("Nav1", "Nav2");
        dropItems.Should().ContainSingle().Which.Label.Should().Be("Drop1");
    }

    [Fact]
    public void GetItems_EmptySection_ReturnsEmptyList()
    {
        var items = new List<MenuItem>
        {
            new()
            {
                Label = "Nav",
                Url = "/nav",
                Section = MenuSection.Navbar,
            },
        };
        var registry = new MenuRegistry(items);

        registry.GetItems(MenuSection.UserDropdown).Should().BeEmpty();
    }

    [Fact]
    public void GetItems_NoItems_ReturnsEmptyList()
    {
        var registry = new MenuRegistry([]);

        registry.GetItems(MenuSection.Navbar).Should().BeEmpty();
        registry.GetItems(MenuSection.UserDropdown).Should().BeEmpty();
    }

    [Fact]
    public void GetItems_PreservesItemProperties()
    {
        var items = new List<MenuItem>
        {
            new()
            {
                Label = "Settings",
                Url = "/settings",
                Icon = "<svg/>",
                Order = 5,
                Section = MenuSection.UserDropdown,
                RequiresAuth = true,
                Group = "account",
            },
        };
        var registry = new MenuRegistry(items);

        var result = registry.GetItems(MenuSection.UserDropdown).Single();

        result.Label.Should().Be("Settings");
        result.Url.Should().Be("/settings");
        result.Icon.Should().Be("<svg/>");
        result.Order.Should().Be(5);
        result.RequiresAuth.Should().BeTrue();
        result.Group.Should().Be("account");
    }

    [Fact]
    public void Constructor_GroupsItemsBySection()
    {
        var items = new List<MenuItem>
        {
            new()
            {
                Label = "A",
                Url = "/a",
                Section = MenuSection.Navbar,
            },
            new()
            {
                Label = "B",
                Url = "/b",
                Section = MenuSection.UserDropdown,
            },
            new()
            {
                Label = "C",
                Url = "/c",
                Section = MenuSection.Navbar,
            },
            new()
            {
                Label = "D",
                Url = "/d",
                Section = MenuSection.UserDropdown,
            },
            new()
            {
                Label = "E",
                Url = "/e",
                Section = MenuSection.UserDropdown,
            },
        };
        var registry = new MenuRegistry(items);

        registry.GetItems(MenuSection.Navbar).Should().HaveCount(2);
        registry.GetItems(MenuSection.UserDropdown).Should().HaveCount(3);
    }

    [Fact]
    public void GetItems_AdminSidebar_ReturnsCorrectItems()
    {
        var items = new List<MenuItem>
        {
            new()
            {
                Label = "Users",
                Url = "/admin/users",
                Section = MenuSection.AdminSidebar,
            },
            new()
            {
                Label = "Nav",
                Url = "/nav",
                Section = MenuSection.Navbar,
            },
        };
        var registry = new MenuRegistry(items);

        var sidebarItems = registry.GetItems(MenuSection.AdminSidebar);

        sidebarItems.Should().ContainSingle().Which.Label.Should().Be("Users");
    }
}
