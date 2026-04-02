using SimpleModule.Agents;
using SimpleModule.AI.Ollama;
using SimpleModule.Hosting;
using SimpleModule.Rag;
using SimpleModule.Rag.StructuredRag;
using SimpleModule.Rag.VectorStore.InMemory;
using SimpleModule.Storage.Local;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddLocalStorage(builder.Configuration);
builder.Services.AddOllamaAI(builder.Configuration);
builder.Services.AddInMemoryVectorStore();
builder.Services.AddSimpleModuleRag(builder.Configuration);
builder.Services.AddStructuredRag(builder.Configuration);
builder.Services.AddSimpleModuleAgents(builder.Configuration);
builder.AddSimpleModule();

var app = builder.Build();
await app.UseSimpleModule();
app.MapDefaultEndpoints();

await app.RunAsync();
