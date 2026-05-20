namespace SimpleModule.Dashboard.Contracts;

public static class DashboardConstants
{
    public const string ModuleName = "Dashboard";
    public const string RoutePrefix = "";
    public const string ViewPrefix = "/";

    public static class Routes
    {
        public static class Views
        {
            public const string Home = "/";
            public const string Broadcasting = "/broadcasting";
        }

        public static class Api
        {
            public const string FireBroadcastTick = "/api/dashboard/broadcasting/tick";
        }
    }
}
