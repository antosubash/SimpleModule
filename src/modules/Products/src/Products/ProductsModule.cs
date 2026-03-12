using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Inertia;
using SimpleModule.Core.Menu;
using SimpleModule.Database;
using SimpleModule.Products.Contracts;

namespace SimpleModule.Products;

[Module(ProductsConstants.ModuleName, RoutePrefix = ProductsConstants.RoutePrefix)]
public class ProductsModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<ProductsDbContext>(configuration, ProductsConstants.ModuleName);
        services.AddScoped<IProductContracts, ProductService>();
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(
            new MenuItem
            {
                Label = "Products",
                Url = "/products/browse",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"/></svg>""",
                Order = 30,
                Section = MenuSection.Navbar,
                RequiresAuth = false,
            }
        );
        menus.Add(
            new MenuItem
            {
                Label = "Manage Products",
                Url = "/products/manage",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/></svg>""",
                Order = 31,
                Section = MenuSection.Navbar,
            }
        );
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/products");

        // Browse page (public)
        group.MapGet(
            "/browse",
            async (IProductContracts products) =>
                Inertia.Render(
                    "Products/Browse",
                    new { products = await products.GetAllProductsAsync() }
                )
        );

        // Manage page
        group.MapGet(
            "/manage",
            async (IProductContracts products) =>
                Inertia.Render(
                    "Products/Manage",
                    new { products = await products.GetAllProductsAsync() }
                )
        );

        // Create page
        group.MapGet("/create", () => Inertia.Render("Products/Create"));

        // Edit page
        group.MapGet(
            "/{id}/edit",
            async (int id, IProductContracts products) =>
            {
                var product = await products.GetProductByIdAsync(id);
                if (product is null)
                    return Results.NotFound();

                return Inertia.Render("Products/Edit", new { product });
            }
        );

        // Create product (POST)
        group.MapPost(
            "/",
            async (HttpContext context, IProductContracts products) =>
            {
                var form = await context.Request.ReadFormAsync();
                var request = new CreateProductRequest
                {
                    Name = form["name"].ToString(),
                    Price = decimal.Parse(form["price"].ToString(), CultureInfo.InvariantCulture),
                };

                await products.CreateProductAsync(request);
                return Results.Redirect("/products/manage");
            }
        );

        // Update product (POST)
        group.MapPost(
            "/{id}",
            async (int id, HttpContext context, IProductContracts products) =>
            {
                var form = await context.Request.ReadFormAsync();
                var request = new UpdateProductRequest
                {
                    Name = form["name"].ToString(),
                    Price = decimal.Parse(form["price"].ToString(), CultureInfo.InvariantCulture),
                };

                await products.UpdateProductAsync(id, request);
                return Results.Redirect($"/products/{id}/edit");
            }
        );

        // Delete product
        group.MapDelete(
            "/{id}",
            async (int id, IProductContracts products) =>
            {
                await products.DeleteProductAsync(id);
                return Results.Redirect("/products/manage");
            }
        );
    }
}
