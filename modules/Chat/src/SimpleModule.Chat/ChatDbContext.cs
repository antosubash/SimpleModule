using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Chat.Contracts;
using SimpleModule.Chat.EntityConfigurations;
using SimpleModule.Database;

namespace SimpleModule.Chat;

public class ChatDbContext(
    DbContextOptions<ChatDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ConversationConfiguration());
        modelBuilder.ApplyConfiguration(new ChatMessageConfiguration());
        modelBuilder.ApplyModuleSchema("Chat", dbOptions.Value);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<ConversationId>()
            .HaveConversion<
                ConversationId.EfCoreValueConverter,
                ConversationId.EfCoreValueComparer
            >();
        configurationBuilder
            .Properties<ChatMessageId>()
            .HaveConversion<
                ChatMessageId.EfCoreValueConverter,
                ChatMessageId.EfCoreValueComparer
            >();
    }
}
