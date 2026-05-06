using System.Globalization;
using Bogus;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Tests.Shared.Fakes;

public static class FakeDataGenerators
{
    public static Faker<UserDto> UserFaker { get; } =
        new Faker<UserDto>()
            .RuleFor(
                u => u.Id,
                f => UserId.From((f.IndexFaker + 1).ToString(CultureInfo.InvariantCulture))
            )
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.DisplayName, f => f.Person.FullName)
            .RuleFor(u => u.EmailConfirmed, _ => true)
            .RuleFor(u => u.TwoFactorEnabled, _ => false);

    public static Faker<CreateUserRequest> CreateUserRequestFaker { get; } =
        new Faker<CreateUserRequest>()
            .RuleFor(r => r.Email, f => f.Internet.Email())
            .RuleFor(r => r.DisplayName, f => f.Person.FullName)
            .RuleFor(r => r.Password, _ => "TestPass1234");

    public static Faker<UpdateUserRequest> UpdateUserRequestFaker { get; } =
        new Faker<UpdateUserRequest>()
            .RuleFor(r => r.Email, f => f.Internet.Email())
            .RuleFor(r => r.DisplayName, f => f.Person.FullName);
}
