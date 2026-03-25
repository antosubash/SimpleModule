using SimpleModule.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddSimpleModule();

var app = builder.Build();
await app.UseSimpleModule();
app.MapDefaultEndpoints();

await app.RunAsync();
