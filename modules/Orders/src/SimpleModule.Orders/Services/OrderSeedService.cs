using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleModule.Orders.Contracts;
using SimpleModule.Users.Constants;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Orders.Services;

public partial class OrderSeedService(
    IServiceProvider serviceProvider,
    ILogger<OrderSeedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

            if (await dbContext.Orders.AnyAsync(cancellationToken))
            {
                return;
            }

            var userManager = scope.ServiceProvider.GetRequiredService<
                UserManager<ApplicationUser>
            >();

            var adminUser = await userManager.FindByEmailAsync(SeedConstants.AdminEmail);
            var regularUser = await userManager.FindByEmailAsync(SeedConstants.UserEmail);

            if (adminUser is null || regularUser is null)
            {
                LogUsersNotFound(logger);
                return;
            }

            LogSeedingOrders(logger);

            var userIds = new[] { adminUser.Id, regularUser.Id };
            var faker = new Faker { Random = new Randomizer(99999) };
            var orders = new List<Order>();

            for (var i = 1; i <= 5; i++)
            {
                var itemCount = faker.Random.Int(1, 3);
                var usedProductIds = new HashSet<int>();
                var items = new List<OrderItem>();
                decimal total = 0;

                for (var j = 0; j < itemCount; j++)
                {
                    int productId;
                    do
                    {
                        productId = faker.Random.Int(1, 10);
                    } while (!usedProductIds.Add(productId));

                    var quantity = faker.Random.Int(1, 5);
                    var price = faker.Random.Decimal(10, 1000);
                    total += price * quantity;

                    items.Add(new OrderItem { ProductId = productId, Quantity = quantity });
                }

                // Distribute orders: odd orders to admin, even to regular user
                var userId = userIds[(i + 1) % 2];

                orders.Add(
                    new Order
                    {
                        UserId = userId,
                        Total = Math.Round(total, 2),
                        CreatedAt = faker.Date.Between(
                            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            new DateTime(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc)
                        ),
                        Items = items,
                    }
                );
            }

            dbContext.Orders.AddRange(orders);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
#pragma warning disable CA1031 // Seed service must not crash the host on database errors
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogSeedError(logger, ex.Message);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeding orders with real user IDs...")]
    private static partial void LogSeedingOrders(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Skipping order seeding: seed users not found yet"
    )]
    private static partial void LogUsersNotFound(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error seeding orders: {ErrorDescription}")]
    private static partial void LogSeedError(ILogger logger, string errorDescription);
}
