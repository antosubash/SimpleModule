namespace SimpleModule.Email.Contracts;

public static class EmailConstants
{
    public const string ModuleName = "Email";
    public const string RoutePrefix = "/api/email";
    public const string ViewPrefix = "/email";

    public static class Routes
    {
        // API endpoints - Messages
        public const string GetAllMessages = "/messages";
        public const string GetMessageById = "/messages/{id}";
        public const string SendEmail = "/messages/send";
        public const string TestSend = "/messages/test-send";

        // API endpoints - Stats
        public const string GetStats = "/stats";

        // API endpoints - Templates
        public const string GetAllTemplates = "/templates";
        public const string GetTemplateById = "/templates/{id}";
        public const string CreateTemplate = "/templates";
        public const string UpdateTemplate = "/templates/{id}";
        public const string DeleteTemplate = "/templates/{id}";

        // View endpoints
        public const string Dashboard = "/dashboard";
        public const string History = "/history";
        public const string Templates = "/templates";
        public const string CreateTemplatePage = "/templates/create";
        public const string EditTemplatePage = "/templates/{id}/edit";
        public const string Settings = "/settings";
    }
}
