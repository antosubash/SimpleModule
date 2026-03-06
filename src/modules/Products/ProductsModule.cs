using SimpleModule.Core;

namespace SimpleModule.Products;

[Module("Products")]
public class ProductsModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/products");

        group.MapGet(
            "/",
            async (IProductService productService) =>
            {
                var products = await productService.GetAllProductsAsync();
                return Results.Ok(products);
            }
        );

        group.MapGet(
            "/{id}",
            async (int id, IProductService productService) =>
            {
                var product = await productService.GetProductByIdAsync(id);
                return product is not null ? Results.Ok(product) : Results.NotFound();
            }
        );
    }
}

public interface IProductService
{
    Task<IEnumerable<Product>> GetAllProductsAsync();
    Task<Product?> GetProductByIdAsync(int id);
}

public class ProductService : IProductService
{
    public Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        return Task.FromResult<IEnumerable<Product>>(
            new[]
            {
                new Product
                {
                    Id = 1,
                    Name = "Laptop",
                    Price = 999.99m,
                },
                new Product
                {
                    Id = 2,
                    Name = "Smartphone",
                    Price = 699.99m,
                },
            }
        );
    }

    public Task<Product?> GetProductByIdAsync(int id)
    {
        return Task.FromResult<Product?>(
            new Product
            {
                Id = id,
                Name = $"Product {id}",
                Price = 100.00m,
            }
        );
    }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}