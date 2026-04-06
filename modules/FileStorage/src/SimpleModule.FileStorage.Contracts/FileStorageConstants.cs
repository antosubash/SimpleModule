namespace SimpleModule.FileStorage.Contracts;

public static class FileStorageConstants
{
    public const string ModuleName = "FileStorage";
    public const string RoutePrefix = "/api/files";
    public const string ViewPrefix = "/files";

    public static class Routes
    {
        // API endpoints
        public const string GetAll = "/";
        public const string Upload = "/";
        public const string GetById = "/{id}";
        public const string Delete = "/{id}";
        public const string Download = "/{id}/download";
        public const string ListFolders = "/folders";

        // View endpoints
        public const string Browse = "/browse";
    }
}
