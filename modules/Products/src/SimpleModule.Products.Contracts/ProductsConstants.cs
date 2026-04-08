namespace SimpleModule.Products.Contracts;

public static class ProductsConstants
{
    public const string ModuleName = "Products";
    public const string RoutePrefix = "/api/products";
    public const string ViewPrefix = "/products";

    public static class Routes
    {
        public const string GetAll = "/";
        public const string Create = "/";
        public const string GetById = "/{id}";
        public const string Update = "/{id}";
        public const string Delete = "/{id}";

        public const string Browse = "/";
        public const string Manage = "/manage";
        public const string CreateView = "/create";
        public const string Edit = "/{id}/edit";
    }
}
