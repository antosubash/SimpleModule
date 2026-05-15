using FluentAssertions;
using SimpleModule.Cli.Commands.Tail;

namespace SimpleModule.Cli.Tests;

public sealed class TailCommandTests
{
    [Fact]
    public void TryParseJson_SerilogCompactLine_ExtractsCoreFields()
    {
        var line =
            """{"@t":"2024-01-02T03:04:05.123Z","@l":"Warning","@mt":"Hello {Name}","Name":"world","SourceContext":"My.App"}""";

        var parsed = LogEntryParser.TryParseJson(line, out var entry);

        parsed.Should().BeTrue();
        entry.Timestamp.Should().NotBeNull();
        entry.Level.Should().Be("Warning");
        entry.Source.Should().Be("My.App");
        entry.Message.Should().Be("Hello {Name}");
        entry.Properties.Should().ContainKey("Name");
        entry.Properties["Name"].Should().Be("world");
    }

    [Fact]
    public void TryParseJson_DotNetJsonConsoleFormatter_ExtractsCategoryStateAndMessage()
    {
        var line =
            """{"Timestamp":"2024-01-02T03:04:05.6789012+00:00","EventId":{"Id":42,"Name":"OrderCreated"},"LogLevel":"Information","Category":"My.App.OrdersController","Message":"Order 7 created","State":{"Message":"Order 7 created","OrderId":7,"{OriginalFormat}":"Order {OrderId} created"}}""";

        var parsed = LogEntryParser.TryParseJson(line, out var entry);

        parsed.Should().BeTrue();
        entry.Level.Should().Be("Information");
        entry.Source.Should().Be("My.App.OrdersController");
        entry.Message.Should().Be("Order 7 created");
        entry.Properties.Should().ContainKey("OrderId");
        entry.Properties["OrderId"].Should().Be("7");
        entry.Properties.Should().ContainKey("EventId.Id");
        entry.Properties["EventId.Id"].Should().Be("42");
    }

    [Fact]
    public void TryParseJson_InvalidJson_ReturnsFalseWithRawMessage()
    {
        const string line = "{ not really json";

        var parsed = LogEntryParser.TryParseJson(line, out var entry);

        parsed.Should().BeFalse();
        entry.Message.Should().Be(line);
    }

    [Fact]
    public void ParsePlain_TimestampLevelSourceMessage_ExtractsParts()
    {
        const string line = "2024-01-02 03:04:05.123 [INF] Foo.Bar: hello world";

        var entry = LogEntryParser.ParsePlain(line);

        entry.Timestamp.Should().NotBeNull();
        entry.Level.Should().Be("Information");
        entry.Source.Should().Be("Foo.Bar");
        entry.Message.Should().Be("hello world");
    }

    [Fact]
    public void Parse_GarbageLine_ReturnsEntryWithRawMessage()
    {
        const string line = "this is not a structured log line";

        var entry = LogEntryParser.Parse(line);

        entry.Message.Should().Be(line);
        entry.Level.Should().BeNull();
        entry.Source.Should().BeNull();
    }

    [Fact]
    public void Parse_RoutesJsonInputThroughJsonParser()
    {
        var line = """{"@l":"Error","@m":"boom"}""";

        var entry = LogEntryParser.Parse(line);

        entry.Level.Should().Be("Error");
        entry.Message.Should().Be("boom");
    }

    [Fact]
    public void Matches_LevelWarning_RejectsInformationLines()
    {
        var settings = new TailSettings { Level = "Warning" };
        var entry = new LogEntry { Level = "Information", Message = "ok" };

        LogEntryFilter.Matches(entry, settings).Should().BeFalse();
    }

    [Fact]
    public void Matches_LevelWarning_AcceptsWarningAndError()
    {
        var settings = new TailSettings { Level = "Warning" };

        LogEntryFilter
            .Matches(new LogEntry { Level = "Warning", Message = "w" }, settings)
            .Should()
            .BeTrue();

        LogEntryFilter
            .Matches(new LogEntry { Level = "Error", Message = "e" }, settings)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Matches_LevelShortForms_AreRecognised()
    {
        var settings = new TailSettings { Level = "warn" };

        LogEntryFilter
            .Matches(new LogEntry { Level = "Error", Message = "e" }, settings)
            .Should()
            .BeTrue();

        LogEntryFilter
            .Matches(new LogEntry { Level = "Information", Message = "i" }, settings)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void Matches_SourcePrefix_MatchesChildCategory()
    {
        var settings = new TailSettings { Source = "Foo.Bar" };
        var entry = new LogEntry { Source = "Foo.Bar.Baz", Message = "x" };

        LogEntryFilter.Matches(entry, settings).Should().BeTrue();
    }

    [Fact]
    public void Matches_SourceMismatch_Rejects()
    {
        var settings = new TailSettings { Source = "Foo.Bar" };
        var entry = new LogEntry { Source = "Other.Namespace", Message = "x" };

        LogEntryFilter.Matches(entry, settings).Should().BeFalse();
    }

    [Fact]
    public void Matches_UserId_MatchesPropertyValue()
    {
        var settings = new TailSettings { User = "42" };
        var props = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["UserId"] = "42",
        };
        var entry = new LogEntry { Message = "x", Properties = props };

        LogEntryFilter.Matches(entry, settings).Should().BeTrue();
    }

    [Fact]
    public void Matches_UserId_RejectsDifferentValue()
    {
        var settings = new TailSettings { User = "42" };
        var props = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["UserId"] = "99",
        };
        var entry = new LogEntry { Message = "x", Properties = props };

        LogEntryFilter.Matches(entry, settings).Should().BeFalse();
    }

    [Fact]
    public void Matches_RequestId_MatchesPropertyCaseInsensitive()
    {
        var settings = new TailSettings { Request = "ABCD-1234" };
        var props = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["requestid"] = "abcd-1234",
        };
        var entry = new LogEntry { Message = "x", Properties = props };

        LogEntryFilter.Matches(entry, settings).Should().BeTrue();
    }

    [Fact]
    public void Matches_FilterSubstring_MatchesInsideMessage()
    {
        var settings = new TailSettings { Filter = "checkout" };
        var entry = new LogEntry { Message = "User started checkout flow" };

        LogEntryFilter.Matches(entry, settings).Should().BeTrue();
    }

    [Fact]
    public void Matches_FilterSubstring_RejectsWhenAbsent()
    {
        var settings = new TailSettings { Filter = "checkout" };
        var entry = new LogEntry { Message = "User signed in" };

        LogEntryFilter.Matches(entry, settings).Should().BeFalse();
    }

    [Fact]
    public void Matches_NoFiltersConfigured_AcceptsEverything()
    {
        var settings = new TailSettings();
        var entry = new LogEntry { Message = "anything" };

        LogEntryFilter.Matches(entry, settings).Should().BeTrue();
    }

    [Fact]
    public void LevelRank_UnknownLevel_ReturnsNegative()
    {
        LogEntryFilter.LevelRank("Bogus").Should().BeLessThan(0);
    }
}
