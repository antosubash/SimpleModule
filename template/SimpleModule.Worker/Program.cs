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
await app.RunAsync();
