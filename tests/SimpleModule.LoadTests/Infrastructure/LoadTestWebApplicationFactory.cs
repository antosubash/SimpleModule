using System.Security.Claims;
using SimpleModule.Admin.Contracts;
using SimpleModule.AuditLogs;
using SimpleModule.FileStorage;
using SimpleModule.OpenIddict.Contracts;
using SimpleModule.Orders;
using SimpleModule.PageBuilder;
using SimpleModule.Products;
using SimpleModule.Tests.Shared.Fixtures;

namespace SimpleModule.LoadTests.Infrastructure;

/// <summary>
/// Extends the test factory with a service account that has all permissions.
/// Runs the full ASP.NET pipeline with SQLite — real routing, auth, EF Core.
/// </summary>
public class LoadTestWebApplicationFactory : SimpleModuleWebApplicationFactory
{
    private static readonly string[] AllPermissions =
    [
        // Products
        ProductsPermissions.View,
        ProductsPermissions.Create,
        ProductsPermissions.Update,
        ProductsPermissions.Delete,
        // Orders
        OrdersPermissions.View,
        OrdersPermissions.Create,
        OrdersPermissions.Update,
        OrdersPermissions.Delete,
        // AuditLogs
        AuditLogsPermissions.View,
        AuditLogsPermissions.Export,
        // FileStorage
        FileStoragePermissions.View,
        FileStoragePermissions.Upload,
        FileStoragePermissions.Delete,
        // PageBuilder
        PageBuilderPermissions.View,
        PageBuilderPermissions.Create,
        PageBuilderPermissions.Update,
        PageBuilderPermissions.Delete,
        PageBuilderPermissions.Publish,
        // Admin
        AdminPermissions.ManageUsers,
        AdminPermissions.ManageRoles,
        AdminPermissions.ViewAuditLog,
        // OpenIddict
        OpenIddictPermissions.ManageClients,
    ];

    /// <summary>
    /// Creates an HttpClient authenticated as a service account with every permission
    /// and the Admin role. This exercises the full auth middleware pipeline.
    /// </summary>
    public HttpClient CreateServiceAccountClient()
    {
        return CreateAuthenticatedClient(
            AllPermissions,
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.NameIdentifier, "service-account"),
            new Claim(ClaimTypes.Name, "Load Test Service Account"),
            new Claim(ClaimTypes.Email, "loadtest@simplemodule.dev")
        );
    }
}
