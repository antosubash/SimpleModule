using System.Text.Json;
using FluentAssertions;
using SimpleModule.Chat.Contracts;

namespace Chat.Tests.Unit;

public sealed class ConversationIdTests
{
    [Fact]
    public void From_WrapsGuidValue()
    {
        var guid = Guid.NewGuid();

        var id = ConversationId.From(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void Equality_IsValueBased()
    {
        var guid = Guid.NewGuid();

        var a = ConversationId.From(guid);
        var b = ConversationId.From(guid);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void DifferentGuidsAreNotEqual()
    {
        var a = ConversationId.From(Guid.NewGuid());
        var b = ConversationId.From(Guid.NewGuid());

        a.Should().NotBe(b);
    }

    [Fact]
    public void JsonSerialization_RoundTripsAsGuidString()
    {
        var id = ConversationId.From(Guid.Parse("11111111-2222-3333-4444-555555555555"));

        var json = JsonSerializer.Serialize(id);
        var deserialized = JsonSerializer.Deserialize<ConversationId>(json);

        json.Should().Contain("11111111-2222-3333-4444-555555555555");
        deserialized.Should().Be(id);
    }
}

public sealed class ChatMessageIdTests
{
    [Fact]
    public void From_WrapsGuidValue()
    {
        var guid = Guid.NewGuid();

        var id = ChatMessageId.From(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void Equality_IsValueBased()
    {
        var guid = Guid.NewGuid();
        ChatMessageId.From(guid).Should().Be(ChatMessageId.From(guid));
    }

    [Fact]
    public void JsonSerialization_RoundTrips()
    {
        var id = ChatMessageId.From(Guid.NewGuid());

        var json = JsonSerializer.Serialize(id);
        var deserialized = JsonSerializer.Deserialize<ChatMessageId>(json);

        deserialized.Should().Be(id);
    }
}
