export const moduleAttrUser = `[Module("Products", RoutePrefix = "/api/products")]
public class ProductsModule : IModule { }`;

export const moduleAttrGenerated = `// Generated at compile time
public static IServiceCollection AddModules(
    this IServiceCollection services)
    => services
        .AddModule<UsersModule>()
        .AddModule<ProductsModule>()
        .AddModule<OrdersModule>()
        /* ... 17 more ... */;`;

export const endpointCode = `public class ListProducts : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/",
                (IProductsContracts p, CancellationToken ct) =>
                    p.GetAllAsync(ct))
            .RequirePermission(ProductsPermissions.View);
}`;

export const crudEndpointsCode = `public class ProductEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapCrud<Product, IProductsContracts>(
            ProductsPermissions.Manage);
}`;

export type CrudVerb = {
  method: string;
  path: string;
  handler: string;
};

export const crudVerbs: readonly CrudVerb[] = [
  { method: 'GET', path: '/api/products', handler: 'ListProducts' },
  { method: 'GET', path: '/api/products/{id}', handler: 'GetProduct' },
  { method: 'POST', path: '/api/products', handler: 'CreateProduct' },
  { method: 'PUT', path: '/api/products/{id}', handler: 'UpdateProduct' },
  { method: 'DELETE', path: '/api/products/{id}', handler: 'DeleteProduct' },
] as const;

export const inertiaCode = `// Server-rendered React — no REST boilerplate
return Inertia.Render("Products/Browse",
    new { products });`;

export const eventBusCode = `// Cross-module events, zero coupling
await _bus.PublishAsync(
    new OrderPlaced(orderId));`;
