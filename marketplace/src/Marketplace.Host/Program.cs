using SimpleModule.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.AddSimpleModule();

var app = builder.Build();
await app.UseSimpleModule();

app.MapGet(
        "/favicon.ico",
        (IWebHostEnvironment env) =>
            Results.File(Path.Combine(env.WebRootPath, "favicon.svg"), "image/svg+xml")
    )
    .ExcludeFromDescription()
    .AllowAnonymous();

await app.RunAsync();
