using FluentAssertions;
using SimpleModule.Chat.Contracts;
using SimpleModule.Core.Exceptions;

namespace Chat.Tests.Unit;

public sealed partial class ChatServiceTests
{
    // ---------- AppendMessageAsync ----------

    [Fact]
    public async Task AppendMessageAsync_UpdatesConversationTimestamp()
    {
        var conv = await _sut.StartConversationAsync("user-1", "assistant", null);
        var originalUpdatedAt = conv.UpdatedAt;

        await Task.Delay(10);
        await _sut.AppendMessageAsync(conv.Id, ChatRole.User, "hello");

        var reloaded = await _sut.GetConversationAsync(conv.Id);
        reloaded!.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        reloaded.Messages.Should().HaveCount(1);
        reloaded.Messages[0].Content.Should().Be("hello");
    }

    [Fact]
    public async Task AppendMessageAsync_PersistsAssistantRoleCorrectly()
    {
        var conv = await _sut.StartConversationAsync("user-1", "assistant", null);

        await _sut.AppendMessageAsync(conv.Id, ChatRole.Assistant, "Hi there!");

        var messages = await _sut.GetMessagesAsync(conv.Id, "user-1");
        messages.Should().HaveCount(1);
        messages[0].Role.Should().Be(ChatRole.Assistant);
        messages[0].Content.Should().Be("Hi there!");
    }

    [Fact]
    public async Task AppendMessageAsync_OrdersChronologically()
    {
        var conv = await _sut.StartConversationAsync("user-1", "assistant", null);

        await _sut.AppendMessageAsync(conv.Id, ChatRole.User, "first");
        await Task.Delay(5);
        await _sut.AppendMessageAsync(conv.Id, ChatRole.Assistant, "second");
        await Task.Delay(5);
        await _sut.AppendMessageAsync(conv.Id, ChatRole.User, "third");

        var messages = await _sut.GetMessagesAsync(conv.Id, "user-1");
        messages.Select(m => m.Content).Should().Equal("first", "second", "third");
    }

    [Fact]
    public async Task AppendMessageAsync_GeneratesUniqueIds()
    {
        var conv = await _sut.StartConversationAsync("user-1", "assistant", null);

        var a = await _sut.AppendMessageAsync(conv.Id, ChatRole.User, "one");
        var b = await _sut.AppendMessageAsync(conv.Id, ChatRole.User, "two");

        a.Id.Should().NotBe(b.Id);
    }

    // ---------- GetMessagesAsync ----------

    [Fact]
    public async Task GetMessagesAsync_EmptyWhenNone()
    {
        var conv = await _sut.StartConversationAsync("user-1", "assistant", null);

        var messages = await _sut.GetMessagesAsync(conv.Id, "user-1");

        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMessagesAsync_ThrowsForNonOwner()
    {
        var conv = await _sut.StartConversationAsync("user-1", "assistant", null);
        await _sut.AppendMessageAsync(conv.Id, ChatRole.User, "secret");

        var act = () => _sut.GetMessagesAsync(conv.Id, "user-2");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetMessagesAsync_IsolatesPerConversation()
    {
        var a = await _sut.StartConversationAsync("user-1", "assistant", null);
        var b = await _sut.StartConversationAsync("user-1", "assistant", null);
        await _sut.AppendMessageAsync(a.Id, ChatRole.User, "a1");
        await _sut.AppendMessageAsync(b.Id, ChatRole.User, "b1");
        await _sut.AppendMessageAsync(a.Id, ChatRole.Assistant, "a2");

        var aMessages = await _sut.GetMessagesAsync(a.Id, "user-1");
        var bMessages = await _sut.GetMessagesAsync(b.Id, "user-1");

        aMessages.Select(m => m.Content).Should().Equal("a1", "a2");
        bMessages.Select(m => m.Content).Should().Equal("b1");
    }
}
