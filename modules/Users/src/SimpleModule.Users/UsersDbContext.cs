using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimpleModule.Database;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users;

public class UsersDbContext(
    DbContextOptions<UsersDbContext> options,
    IOptions<DatabaseOptions> dbOptions
) : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
{
    // Opt into Identity Schema Version 3 to provision the AspNetUserPasskeys table.
    protected override Version SchemaVersion => IdentitySchemaVersions.Version3;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyModuleSchema("Users", dbOptions.Value);
    }
}
