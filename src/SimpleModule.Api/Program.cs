using SimpleModule.Core;
using SimpleModule.Core.Events;
using SimpleModule.Core.Exceptions;
using SimpleModule.Database;
using SimpleModule.Database.Health;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Register event bus
builder.Services.AddScoped<IEventBus, EventBus>();

// Register all modules
builder.Services.AddModules(builder.Configuration);

// Health checks
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

var app = builder.Build();

// Ensure databases are created with seed data
app.EnsureModuleDatabases();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Health endpoints
app.MapHealthChecks("/health");

// Automatically map all module endpoints
app.MapModuleEndpoints();

app.Run();
