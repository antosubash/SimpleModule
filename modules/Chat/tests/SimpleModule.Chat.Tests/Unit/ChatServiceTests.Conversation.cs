using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Chat.Contracts;
using SimpleModule.Core.Exceptions;

namespace Chat.Tests.Unit;

public sealed partial class ChatServiceTests
{
    // ---------- StartConversationAsync ----------

    [Fact]
    public async Task StartConversationAsync_PersistsWithDefaultTitle()
    {
        var conv = await _sut.StartConversationAsync("user-1", "assistant", null);

        conv.UserId.Should().Be("user-1");
        conv.AgentName.Should().Be("assistant");
        conv.Title.Should().Be("New conversation");
        conv.Id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task StartConversationAsync_TrimsTitle()
    {
        var conv = await _sut.StartConversationAsync("user-1", "assistant", "  Hello   ");

        conv.Title.Should().Be("Hello");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public async Task StartConversationAsync_WhitespaceTitleDefaults(string title)
    {
        var conv = await _sut.StartConversationAsync("user-1", "assistant", title);

        conv.Title.Should().Be("New conversation");
    }

    [Fact]
    public async Task StartConversationAsync_SetsCreatedAndUpdatedEqual()
    {
        var conv = await _sut.StartConversationAsync("user-1", "assistant", null);

        conv.CreatedAt.Should().Be(conv.UpdatedAt);
    }

    [Fact]
    public async Task StartConversationAsync_IsPersistedToDatabase()
    {
        var conv = await _sut.StartConversationAsync("user-1", "assistant", null);

        var reloaded = await _db.Conversations.FindAsync(conv.Id);
        reloaded.Should().NotBeNull();
        reloaded!.Id.Should().Be(conv.Id);
    }

    // ---------- GetUserConversationsAsync ----------

    [Fact]
    public async Task GetUserConversationsAsync_ReturnsOnlyOwnConversations()
    {
        await _sut.StartConversationAsync("user-1", "assistant", "mine");
        await _sut.StartConversationAsync("user-2", "assistant", "theirs");

        var results = await _sut.GetUserConversationsAsync("user-1");

        results.Should().HaveCount(1);
        results[0].Title.Should().Be("mine");
    }

    [Fact]
    public async Task GetUserConversationsAsync_EmptyForNewUser()
    {
        var results = await _sut.GetUserConversationsAsync("never-seen");

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserConversationsAsync_OrdersPinnedFirst()
    {
        var a = await _sut.StartConversationAsync("user-1", "assistant", "first");
        var b = await _sut.StartConversationAsync("user-1", "assistant", "second");

        var tracked = await _db.Conversations.FirstAsync(c => c.Id == a.Id);
        tracked.Pinned = true;
        await _db.SaveChangesAsync();

        var results = await _sut.GetUserConversationsAsync("user-1");

        results.Should().HaveCount(2);
        results[0].Id.Should().Be(a.Id);
        results[1].Id.Should().Be(b.Id);
    }

    [Fact]
    public async Task GetUserConversationsAsync_OrdersByUpdatedAtDescendingWithinSamePinState()
    {
        var older = await _sut.StartConversationAsync("user-1", "assistant", "older");
        await Task.Delay(10);
        var newer = await _sut.StartConversationAsync("user-1", "assistant", "newer");

        var results = await _sut.GetUserConversationsAsync("user-1");

        results[0].Id.Should().Be(newer.Id);
        results[1].Id.Should().Be(older.Id);
    }

    // ---------- GetConversationAsync ----------

    [Fact]
    public async Task GetConversationAsync_ReturnsNullWhenMissing()
    {
        var result = await _sut.GetConversationAsync(ConversationId.From(Guid.NewGuid()));

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetConversationAsync_IncludesMessagesInOrder()
    {
        var conv = await _sut.StartConversationAsync("user-1", "assistant", null);
        await _sut.AppendMessageAsync(conv.Id, ChatRole.User, "a");
        await Task.Delay(5);
        await _sut.AppendMessageAsync(conv.Id, ChatRole.Assistant, "b");
        await Task.Delay(5);
        await _sut.AppendMessageAsync(conv.Id, ChatRole.User, "c");

        var reloaded = await _sut.GetConversationAsync(conv.Id);

        reloaded!.Messages.Select(m => m.Content).Should().Equal("a", "b", "c");
    }

    // ---------- LoadOwnedAsync ----------

    [Fact]
    public async Task LoadOwnedAsync_ReturnsConversationForOwner()
    {
        var conv = await _sut.StartConversationAsync("user-1", "assistant", null);

        var loaded = await _sut.LoadOwnedAsync(conv.Id, "user-1");

        loaded.Id.Should().Be(conv.Id);
    }

    [Fact]
    public async Task LoadOwnedAsync_ThrowsWhenAnotherUser()
    {
        var conv = await _sut.StartConversationAsync("user-1", "assistant", null);

        var act = () => _sut.LoadOwnedAsync(conv.Id, "user-2");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task LoadOwnedAsync_ThrowsWhenConversationMissing()
    {
        var act = () => _sut.LoadOwnedAsync(ConversationId.From(Guid.NewGuid()), "user-1");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------- RenameAsync ----------

    [Fact]
    public async Task RenameAsync_UpdatesTitle()
    {
        var conv = await _sut.StartConversationAsync("user-1", "assistant", null);

        var updated = await _sut.RenameAsync(conv.Id, "user-1", "A better title");

        updated.Title.Should().Be("A better title");
    }

    [Fact]
    public async Task RenameAsync_TrimsWhitespace()
    {
        var conv = await _sut.StartConversationAsync("user-1", "assistant", null);

        var updated = await _sut.RenameAsync(conv.Id, "user-1", "   trimmed  ");

        updated.Title.Should().Be("trimmed");
    }

    [Fact]
    public async Task RenameAsync_ThrowsForNonOwner()
    {
        var conv = await _sut.StartConversationAsync("user-1", "assistant", null);

        var act = () => _sut.RenameAsync(conv.Id, "user-2", "hijack");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RenameAsync_BumpsUpdatedAt()
    {
        var conv = await _sut.StartConversationAsync("user-1", "assistant", null);
        var before = conv.UpdatedAt;
        await Task.Delay(10);

        var after = await _sut.RenameAsync(conv.Id, "user-1", "renamed");

        after.UpdatedAt.Should().BeAfter(before);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_RemovesConversationAndMessages()
    {
        var conv = await _sut.StartConversationAsync("user-1", "assistant", null);
        await _sut.AppendMessageAsync(conv.Id, ChatRole.User, "hi");

        await _sut.DeleteAsync(conv.Id, "user-1");

        var found = await _sut.GetConversationAsync(conv.Id);
        found.Should().BeNull();
        (await _db.ChatMessages.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsForNonOwner()
    {
        var conv = await _sut.StartConversationAsync("user-1", "assistant", null);

        var act = () => _sut.DeleteAsync(conv.Id, "user-2");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_OnlyDeletesMessagesFromThatConversation()
    {
        var keep = await _sut.StartConversationAsync("user-1", "assistant", "keep");
        var drop = await _sut.StartConversationAsync("user-1", "assistant", "drop");
        await _sut.AppendMessageAsync(keep.Id, ChatRole.User, "keep-me");
        await _sut.AppendMessageAsync(drop.Id, ChatRole.User, "drop-me");

        await _sut.DeleteAsync(drop.Id, "user-1");

        var remaining = await _db.ChatMessages.ToListAsync();
        remaining.Should().HaveCount(1);
        remaining[0].Content.Should().Be("keep-me");
    }
}
