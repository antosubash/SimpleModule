using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Agents;
using SimpleModule.AI.Ollama;
using SimpleModule.Core;
using SimpleModule.Database;
using SimpleModule.Hosting;
using SimpleModule.Rag;
using SimpleModule.Rag.StructuredRag;
using SimpleModule.Rag.VectorStore.InMemory;
using SimpleModule.Storage.Local;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddLocalStorage(builder.Configuration);
builder.Services.AddOllamaAI(builder.Configuration);
builder.Services.AddInMemoryVectorStore();
builder.Services.AddSimpleModuleRag(builder.Configuration);
builder.Services.AddStructuredRag(builder.Configuration);
builder.Services.AddSimpleModuleAgents(builder.Configuration);
builder.AddSimpleModuleWorker();

// Source-generated: registers all module services and lifecycle hosting.
builder.Services.AddModules(builder.Configuration);

// Register module settings definitions (required by SettingsService / AuditLogs interceptor).
builder.Services.CollectModuleSettings();

// Register module agent definitions.
builder.Services.AddModuleAgents();

var app = builder.Build();

// Ensure all module database schemas exist before hosted services start.
// This mirrors UseSimpleModuleInfrastructure in the web host.
using (var scope = app.Services.CreateScope())
{
    var infos = scope.ServiceProvider.GetServices<ModuleDbContextInfo>();
    foreach (var info in infos)
    {
        if (scope.ServiceProvider.GetService(info.DbContextType) is not DbContext db)
            continue;

        // EnsureCreated returns true only if the DB was just created.
        // When multiple contexts share a SQLite file, only the first call creates
        // the file; subsequent calls return false without creating their tables.
        // We use EnsureCreated then fall back to CreateTablesAsync() for subsequent contexts.
        if (!await db.Database.EnsureCreatedAsync())
        {
            var creator = db.GetService<IRelationalDatabaseCreator>();
            try { await creator.CreateTablesAsync(); }
#pragma warning disable CA1031
            catch { /* tables already exist */ }
#pragma warning restore CA1031
        }
    }
}

await app.RunAsync();
