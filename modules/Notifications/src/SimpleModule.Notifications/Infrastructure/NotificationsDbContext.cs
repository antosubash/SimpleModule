using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Notifications.Infrastructure.EntityConfigurations;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Notifications.Infrastructure;

public class NotificationsDbContext(
    DbContextOptions<NotificationsDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new NotificationConfiguration());
        modelBuilder.ApplyModuleSchema("Notifications", dbOptions.Value);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<NotificationId>()
            .HaveConversion<
                NotificationId.EfCoreValueConverter,
                NotificationId.EfCoreValueComparer
            >();
        configurationBuilder
            .Properties<UserId>()
            .HaveConversion<UserId.EfCoreValueConverter, UserId.EfCoreValueComparer>();

        if (dbOptions.Value.DetectProvider("Notifications") == DatabaseProvider.Sqlite)
        {
            configurationBuilder
                .Properties<DateTimeOffset>()
                .HaveConversion<DateTimeOffsetToBinaryConverter>();
            configurationBuilder
                .Properties<DateTimeOffset?>()
                .HaveConversion<DateTimeOffsetToBinaryConverter>();
        }
    }
}
