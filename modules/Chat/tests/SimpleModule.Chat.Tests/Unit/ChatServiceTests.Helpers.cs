using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimpleModule.Chat;
using SimpleModule.Database;

namespace Chat.Tests.Unit;

public sealed partial class ChatServiceTests : IDisposable
{
    private readonly ChatDbContext _db;
    private readonly ChatService _sut;

    public ChatServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var dbOptions = Options.Create(
            new DatabaseOptions
            {
                ModuleConnections = new Dictionary<string, string>
                {
                    ["Chat"] = "Data Source=:memory:",
                },
            }
        );
        _db = new ChatDbContext(options, dbOptions);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
        _sut = new ChatService(_db, NullLogger<ChatService>.Instance);
    }

    public void Dispose() => _db.Dispose();
}
