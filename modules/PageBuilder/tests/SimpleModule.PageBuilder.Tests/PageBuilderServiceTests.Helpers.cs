using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.PageBuilder;
using SimpleModule.Tests.Shared.Fakes;

namespace PageBuilder.Tests;

public sealed partial class PageBuilderServiceTests : IDisposable
{
    private readonly PageBuilderDbContext _db;
    private readonly PageBuilderService _sut;

    public PageBuilderServiceTests()
    {
        var options = new DbContextOptionsBuilder<PageBuilderDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var dbOptions = Options.Create(
            new DatabaseOptions
            {
                ModuleConnections = new Dictionary<string, string>
                {
                    ["PageBuilder"] = "Data Source=:memory:",
                },
            }
        );
        _db = new PageBuilderDbContext(options, dbOptions);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
        _sut = new PageBuilderService(
            _db,
            new TestMessageBus(),
            NullLogger<PageBuilderService>.Instance
        );
    }

    public void Dispose() => _db.Dispose();
}
