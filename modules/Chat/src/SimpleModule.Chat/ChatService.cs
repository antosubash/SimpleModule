using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.Chat.Contracts;
using SimpleModule.Core.Exceptions;

namespace SimpleModule.Chat;

public partial class ChatService(ChatDbContext db, ILogger<ChatService> logger) : IChatContracts
{
    public async Task<IReadOnlyList<Conversation>> GetUserConversationsAsync(
        string userId,
        CancellationToken cancellationToken = default
    ) =>
        await db
            .Conversations.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.Pinned)
            .ThenByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);

    public async Task<Conversation?> GetConversationAsync(
        ConversationId id,
        CancellationToken cancellationToken = default
    ) =>
        await db
            .Conversations.AsNoTracking()
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Conversation> StartConversationAsync(
        string userId,
        string agentName,
        string? title,
        CancellationToken cancellationToken = default
    )
    {
        var now = DateTimeOffset.UtcNow;
        var conversation = new Conversation
        {
            Id = ConversationId.From(Guid.NewGuid()),
            UserId = userId,
            AgentName = agentName,
            Title = string.IsNullOrWhiteSpace(title) ? "New conversation" : title.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Conversations.Add(conversation);
        await db.SaveChangesAsync(cancellationToken);

        LogConversationCreated(logger, conversation.Id.Value, userId, agentName);
        return conversation;
    }

    public async Task<Conversation> RenameAsync(
        ConversationId id,
        string userId,
        string title,
        CancellationToken cancellationToken = default
    )
    {
        var conversation = await LoadOwnedAsync(id, userId, cancellationToken);
        conversation.Title = title.Trim();
        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    public async Task DeleteAsync(
        ConversationId id,
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        var conversation = await LoadOwnedAsync(id, userId, cancellationToken);
        db.Conversations.Remove(conversation);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(
        ConversationId id,
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        _ = await LoadOwnedAsync(id, userId, cancellationToken);
        return await db
            .ChatMessages.AsNoTracking()
            .Where(m => m.ConversationId == id)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatMessage> AppendMessageAsync(
        ConversationId conversationId,
        ChatRole role,
        string content,
        CancellationToken cancellationToken = default
    )
    {
        var message = new ChatMessage
        {
            Id = ChatMessageId.From(Guid.NewGuid()),
            ConversationId = conversationId,
            Role = role,
            Content = content,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.ChatMessages.Add(message);

        var conversation = await db.Conversations.FirstOrDefaultAsync(
            c => c.Id == conversationId,
            cancellationToken
        );
        if (conversation is not null)
        {
            conversation.UpdatedAt = message.CreatedAt;
        }

        await db.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task<Conversation> LoadOwnedAsync(
        ConversationId id,
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        var conversation =
            await db.Conversations.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("Conversation", id.Value);
        if (!string.Equals(conversation.UserId, userId, StringComparison.Ordinal))
        {
            throw new NotFoundException("Conversation", id.Value);
        }
        return conversation;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Conversation {ConversationId} created by {UserId} for agent {AgentName}"
    )]
    private static partial void LogConversationCreated(
        ILogger logger,
        Guid conversationId,
        string userId,
        string agentName
    );
}
