using System.Text.Json;
using BenchmarkDotNet.Attributes;
using SimpleModule.Orders.Contracts;
using SimpleModule.Products.Contracts;
using SimpleModule.Tests.Shared.Fakes;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 20)]
public class SerializationBenchmarks
{
    private Product _product = null!;
    private Order _order = null!;
    private UserDto _user = null!;
    private List<Product> _productList = null!;
    private List<Order> _orderList = null!;
    private string _productJson = null!;
    private string _orderJson = null!;
    private string _userJson = null!;
    private string _productListJson = null!;

    [GlobalSetup]
    public void Setup()
    {
        _product = FakeDataGenerators.ProductFaker.Generate();
        _order = FakeDataGenerators.OrderFaker.Generate();
        _user = FakeDataGenerators.UserFaker.Generate();
        _productList = FakeDataGenerators.ProductFaker.Generate(100);
        _orderList = FakeDataGenerators.OrderFaker.Generate(100);

        _productJson = JsonSerializer.Serialize(_product);
        _orderJson = JsonSerializer.Serialize(_order);
        _userJson = JsonSerializer.Serialize(_user);
        _productListJson = JsonSerializer.Serialize(_productList);
    }

    [Benchmark]
    public string SerializeProduct() => JsonSerializer.Serialize(_product);

    [Benchmark]
    public string SerializeOrder() => JsonSerializer.Serialize(_order);

    [Benchmark]
    public string SerializeUser() => JsonSerializer.Serialize(_user);

    [Benchmark]
    public string SerializeProductList() => JsonSerializer.Serialize(_productList);

    [Benchmark]
    public string SerializeOrderList() => JsonSerializer.Serialize(_orderList);

    [Benchmark]
    public Product? DeserializeProduct() => JsonSerializer.Deserialize<Product>(_productJson);

    [Benchmark]
    public Order? DeserializeOrder() => JsonSerializer.Deserialize<Order>(_orderJson);

    [Benchmark]
    public UserDto? DeserializeUser() => JsonSerializer.Deserialize<UserDto>(_userJson);

    [Benchmark]
    public List<Product>? DeserializeProductList() =>
        JsonSerializer.Deserialize<List<Product>>(_productListJson);
}
