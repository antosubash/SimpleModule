namespace SimpleModule.Orders;

public static class OrdersConstants
{
    public const string ModuleName = "Orders";
    public const string RoutePrefix = "/api/orders";

    public static class Fields
    {
        public const string UserId = "UserId";
        public const string Items = "Items";
    }

    public static class ValidationMessages
    {
        public const string UserIdRequired = "UserId is required.";
        public const string AtLeastOneItemRequired = "At least one item is required.";
        public const string QuantityMustBePositiveFormat = "Items[{0}].Quantity must be greater than 0.";
    }
}
