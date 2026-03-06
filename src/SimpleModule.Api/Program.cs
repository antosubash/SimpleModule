using SimpleModule.Core;
using SimpleModule.Orders;
using SimpleModule.Products;
using SimpleModule.Users;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register all modules
builder.Services.AddModules(builder.Configuration);

var app = builder.Build();

// Ensure databases are created with seed data
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<UsersDbContext>().Database.EnsureCreated();
    scope.ServiceProvider.GetRequiredService<ProductsDbContext>().Database.EnsureCreated();
    scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.EnsureCreated();
}

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
