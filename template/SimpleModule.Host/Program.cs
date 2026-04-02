using SimpleModule.Hosting;
using SimpleModule.Storage.Local;
using TickerQ.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddLocalStorage(builder.Configuration);
builder.AddSimpleModule();

var app = builder.Build();
await app.UseSimpleModule();
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseTickerQ();
}
app.MapDefaultEndpoints();

await app.RunAsync();
