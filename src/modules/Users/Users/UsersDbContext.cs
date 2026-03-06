using Bogus;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Users;

public class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Name).IsRequired();
        });

        modelBuilder.Entity<User>().HasData(GenerateSeedUsers());
    }

    private static User[] GenerateSeedUsers()
    {
        var id = 0;
        var faker = new Faker<User>()
            .UseSeed(12345)
            .RuleFor(u => u.Id, _ => ++id)
            .RuleFor(u => u.Name, f => f.Name.FullName());

        return faker.Generate(10).ToArray();
    }
}
