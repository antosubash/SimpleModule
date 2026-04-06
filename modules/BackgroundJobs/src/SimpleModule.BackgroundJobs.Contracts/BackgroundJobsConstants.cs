namespace SimpleModule.BackgroundJobs.Contracts;

public static class BackgroundJobsConstants
{
    public const string ModuleName = "BackgroundJobs";
    public const string RoutePrefix = "/api/jobs";
    public const string ViewPrefix = "/admin/jobs";

    public static class Routes
    {
        // API endpoints
        public const string GetAll = "/";
        public const string GetById = "/{id:guid}";
        public const string Cancel = "/{id:guid}/cancel";
        public const string Retry = "/{id:guid}/retry";
        public const string GetRecurring = "/recurring";
        public const string DeleteRecurring = "/recurring/{id:guid}";
        public const string ToggleRecurring = "/recurring/{id:guid}/toggle";

        // View endpoints
        public const string Dashboard = "/";
        public const string List = "/list";
        public const string Detail = "/{id:guid}";
        public const string Recurring = "/recurring";
    }
}
