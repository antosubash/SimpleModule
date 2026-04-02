using SimpleModule.Hosting;
using SimpleModule.Storage.Local;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddLocalStorage(builder.Configuration);
builder.AddSimpleModule();

var app = builder.Build();
await app.UseSimpleModule();
app.MapDefaultEndpoints();

await app.RunAsync();
