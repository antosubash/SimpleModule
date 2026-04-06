namespace SimpleModule.Orders.Contracts;

public static class OrdersConstants
{
    public const string ModuleName = "Orders";
    public const string RoutePrefix = "/api/orders";
    public const string ViewPrefix = "/orders";

    public static class Routes
    {
        public const string GetAll = "/";
        public const string Create = "/";
        public const string GetById = "/{id}";
        public const string Update = "/{id}";
        public const string Delete = "/{id}";

        public const string List = "/";
        public const string CreateView = "/create";
        public const string Edit = "/{id}/edit";
    }

    public static class Fields
    {
        public const string UserId = "UserId";
        public const string Items = "Items";
    }

    public static class ValidationMessages
    {
        public const string UserIdRequired = "UserId is required.";
        public const string AtLeastOneItemRequired = "At least one item is required.";
        public const string QuantityMustBePositiveFormat =
            "Items[{0}].Quantity must be greater than 0.";
    }
}
