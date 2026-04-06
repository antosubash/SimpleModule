namespace SimpleModule.PageBuilder.Contracts;

public static class PageBuilderConstants
{
    public const string ModuleName = "PageBuilder";
    public const string RoutePrefix = "/api/pagebuilder";
    public const string ViewPrefix = "/pages";

    public static class Routes
    {
        // API endpoints — Pages
        public const string GetAll = "/";
        public const string Create = "/";
        public const string GetById = "/{id}";
        public const string Update = "/{id}";
        public const string UpdateContent = "/{id}/content";
        public const string Delete = "/{id}";
        public const string PermanentDelete = "/{id}/permanent";
        public const string Publish = "/{id}/publish";
        public const string Unpublish = "/{id}/unpublish";
        public const string Restore = "/{id}/restore";
        public const string Trash = "/trash";

        // API endpoints — Tags
        public const string AddTag = "/{id}/tags";
        public const string GetAllTags = "/tags";
        public const string RemoveTag = "/{id}/tags/{tagId}";

        // API endpoints — Templates
        public const string CreateTemplate = "/templates";
        public const string DeleteTemplate = "/templates/{id}";
        public const string GetAllTemplates = "/templates";

        // View endpoints
        public const string Manage = "/manage";
        public const string PagesList = "/";
        public const string Editor = "/new";
        public const string EditPage = "/{id}/edit";
        public const string Viewer = "/view/{slug}";
        public const string ViewerDraft = "/view/{slug}/draft";
    }
}
