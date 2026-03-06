using SimpleModule.Core;
using SimpleModule.Database;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register all modules
builder.Services.AddModules(builder.Configuration);

var app = builder.Build();

// Ensure databases are created with seed data
app.EnsureModuleDatabases();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Automatically map all module endpoints
app.MapModuleEndpoints();

app.Run();
