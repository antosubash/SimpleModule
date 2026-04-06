using SimpleModule.Orders.Contracts;

namespace SimpleModule.Orders;

public static class OrdersConstants
{
    public const string ModuleName = Contracts.OrdersConstants.ModuleName;
    public const string RoutePrefix = Contracts.OrdersConstants.RoutePrefix;
    public const string ViewPrefix = Contracts.OrdersConstants.ViewPrefix;

    public static class Routes
    {
        public const string GetAll = Contracts.OrdersConstants.Routes.GetAll;
        public const string Create = Contracts.OrdersConstants.Routes.Create;
        public const string GetById = Contracts.OrdersConstants.Routes.GetById;
        public const string Update = Contracts.OrdersConstants.Routes.Update;
        public const string Delete = Contracts.OrdersConstants.Routes.Delete;
        public const string List = Contracts.OrdersConstants.Routes.List;
        public const string CreateView = Contracts.OrdersConstants.Routes.CreateView;
        public const string Edit = Contracts.OrdersConstants.Routes.Edit;
    }

    public static class Fields
    {
        public const string UserId = Contracts.OrdersConstants.Fields.UserId;
        public const string Items = Contracts.OrdersConstants.Fields.Items;
    }

    public static class ValidationMessages
    {
        public const string UserIdRequired = Contracts
            .OrdersConstants
            .ValidationMessages
            .UserIdRequired;
        public const string AtLeastOneItemRequired = Contracts
            .OrdersConstants
            .ValidationMessages
            .AtLeastOneItemRequired;
        public const string QuantityMustBePositiveFormat = Contracts
            .OrdersConstants
            .ValidationMessages
            .QuantityMustBePositiveFormat;
    }
}
