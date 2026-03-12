using System.Globalization;
using Bogus;
using SimpleModule.Orders.Contracts;
using SimpleModule.Products.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Tests.Shared.Fakes;

public static class FakeDataGenerators
{
    public static Faker<UserDto> UserFaker { get; } =
        new Faker<UserDto>()
            .RuleFor(u => u.Id, f => (f.IndexFaker + 1).ToString(CultureInfo.InvariantCulture))
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.DisplayName, f => f.Person.FullName)
            .RuleFor(u => u.EmailConfirmed, _ => true)
            .RuleFor(u => u.TwoFactorEnabled, _ => false);

    public static Faker<Product> ProductFaker { get; } =
        new Faker<Product>()
            .RuleFor(p => p.Id, f => f.IndexFaker + 1)
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Price, f => f.Finance.Amount(1, 1000));

    public static Faker<OrderItem> OrderItemFaker { get; } =
        new Faker<OrderItem>()
            .RuleFor(oi => oi.ProductId, f => f.Random.Int(1, 100))
            .RuleFor(oi => oi.Quantity, f => f.Random.Int(1, 10));

    public static Faker<Order> OrderFaker { get; } =
        new Faker<Order>()
            .RuleFor(o => o.Id, f => f.IndexFaker + 1)
            .RuleFor(
                o => o.UserId,
                f => f.Random.Int(1, 100).ToString(CultureInfo.InvariantCulture)
            )
            .RuleFor(o => o.Items, f => OrderItemFaker.Generate(f.Random.Int(1, 3)))
            .RuleFor(o => o.Total, f => f.Finance.Amount(10, 500))
            .RuleFor(o => o.CreatedAt, f => f.Date.Recent());

    public static Faker<CreateOrderRequest> CreateOrderRequestFaker { get; } =
        new Faker<CreateOrderRequest>()
            .RuleFor(
                r => r.UserId,
                f => f.Random.Int(1, 100).ToString(CultureInfo.InvariantCulture)
            )
            .RuleFor(r => r.Items, f => OrderItemFaker.Generate(f.Random.Int(1, 3)));

    public static Faker<UpdateOrderRequest> UpdateOrderRequestFaker { get; } =
        new Faker<UpdateOrderRequest>()
            .RuleFor(
                r => r.UserId,
                f => f.Random.Int(1, 100).ToString(CultureInfo.InvariantCulture)
            )
            .RuleFor(r => r.Items, f => OrderItemFaker.Generate(f.Random.Int(1, 3)));

    public static Faker<CreateProductRequest> CreateProductRequestFaker { get; } =
        new Faker<CreateProductRequest>()
            .RuleFor(r => r.Name, f => f.Commerce.ProductName())
            .RuleFor(r => r.Price, f => f.Finance.Amount(1, 1000));

    public static Faker<UpdateProductRequest> UpdateProductRequestFaker { get; } =
        new Faker<UpdateProductRequest>()
            .RuleFor(r => r.Name, f => f.Commerce.ProductName())
            .RuleFor(r => r.Price, f => f.Finance.Amount(1, 1000));

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
