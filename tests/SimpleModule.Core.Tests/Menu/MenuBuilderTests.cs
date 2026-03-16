using FluentAssertions;
using SimpleModule.Core.Menu;

namespace SimpleModule.Core.Tests.Menu;

public class MenuBuilderTests
{
    [Fact]
    public void Add_SingleItem_ReturnsItInToList()
    {
        var builder = new MenuBuilder();
        var item = new MenuItem { Label = "Users", Url = "/users" };

        builder.Add(item);

        builder.ToList().Should().ContainSingle().Which.Should().BeSameAs(item);
    }

    [Fact]
    public void Add_ReturnsSelf_ForChaining()
    {
        var builder = new MenuBuilder();

        var result = builder.Add(new MenuItem { Label = "A", Url = "/a" });

        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void ToList_ReturnsSortedByOrder()
    {
        var builder = new MenuBuilder();
        builder.Add(
            new MenuItem
            {
                Label = "C",
                Url = "/c",
                Order = 30,
            }
        );
        builder.Add(
            new MenuItem
            {
                Label = "A",
                Url = "/a",
                Order = 10,
            }
        );
        builder.Add(
            new MenuItem
            {
                Label = "B",
                Url = "/b",
                Order = 20,
            }
        );

        var items = builder.ToList();

        items.Select(i => i.Label).Should().ContainInOrder("A", "B", "C");
    }

    [Fact]
    public void ToList_Empty_ReturnsEmptyList()
    {
        var builder = new MenuBuilder();

        builder.ToList().Should().BeEmpty();
    }

    [Fact]
    public void ToList_SameOrder_PreservesInsertionOrder()
    {
        var builder = new MenuBuilder();
        builder.Add(
            new MenuItem
            {
                Label = "First",
                Url = "/first",
                Order = 10,
            }
        );
        builder.Add(
            new MenuItem
            {
                Label = "Second",
                Url = "/second",
                Order = 10,
            }
        );

        var items = builder.ToList();

        items.Select(i => i.Label).Should().ContainInOrder("First", "Second");
    }

    [Fact]
    public void Chaining_MultipleAdds_AllPresent()
    {
        var builder = new MenuBuilder();

        builder
            .Add(
                new MenuItem
                {
                    Label = "A",
                    Url = "/a",
                    Order = 1,
                }
            )
            .Add(
                new MenuItem
                {
                    Label = "B",
                    Url = "/b",
                    Order = 2,
                }
            )
            .Add(
                new MenuItem
                {
                    Label = "C",
                    Url = "/c",
                    Order = 3,
                }
            );

        builder.ToList().Should().HaveCount(3);
    }
}
