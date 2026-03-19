using Microsoft.EntityFrameworkCore;
using SimpleModule.Orders.Contracts;
using SimpleModule.PageBuilder.Contracts;
using SimpleModule.Permissions.Contracts;
using SimpleModule.Products.Contracts;
using SimpleModule.Users.Contracts;

namespace SimpleModule.Host;

public partial class HostDbContext
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<ProductId>()
            .HaveConversion<ProductId.EfCoreValueConverter, ProductId.EfCoreValueComparer>();
        configurationBuilder
            .Properties<OrderId>()
            .HaveConversion<OrderId.EfCoreValueConverter, OrderId.EfCoreValueComparer>();
        configurationBuilder
            .Properties<PageId>()
            .HaveConversion<PageId.EfCoreValueConverter, PageId.EfCoreValueComparer>();
        configurationBuilder
            .Properties<PageTemplateId>()
            .HaveConversion<PageTemplateId.EfCoreValueConverter, PageTemplateId.EfCoreValueComparer>();
        configurationBuilder
            .Properties<PageTagId>()
            .HaveConversion<PageTagId.EfCoreValueConverter, PageTagId.EfCoreValueComparer>();
        configurationBuilder
            .Properties<UserId>()
            .HaveConversion<UserId.EfCoreValueConverter, UserId.EfCoreValueComparer>();
        configurationBuilder
            .Properties<RoleId>()
            .HaveConversion<RoleId.EfCoreValueConverter, RoleId.EfCoreValueComparer>();
    }
}
