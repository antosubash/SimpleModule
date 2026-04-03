using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Email.Contracts;
using SimpleModule.Email.EntityConfigurations;

namespace SimpleModule.Email;

public class EmailDbContext(
    DbContextOptions<EmailDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : DbContext(options)
{
    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new EmailMessageConfiguration());
        modelBuilder.ApplyConfiguration(new EmailTemplateConfiguration());
        modelBuilder.ApplyModuleSchema("Email", dbOptions.Value);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<EmailMessageId>()
            .HaveConversion<
                EmailMessageId.EfCoreValueConverter,
                EmailMessageId.EfCoreValueComparer
            >();
        configurationBuilder
            .Properties<EmailTemplateId>()
            .HaveConversion<
                EmailTemplateId.EfCoreValueConverter,
                EmailTemplateId.EfCoreValueComparer
            >();
    }
}
