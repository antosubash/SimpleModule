using SimpleModule.Core.Authorization;

namespace SimpleModule.PageBuilder;

public sealed class PageBuilderPermissions : IModulePermissions
{
    public const string View = "PageBuilder.View";
    public const string Create = "PageBuilder.Create";
    public const string Update = "PageBuilder.Update";
    public const string Delete = "PageBuilder.Delete";
    public const string Publish = "PageBuilder.Publish";
}
