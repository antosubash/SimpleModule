namespace SimpleModule.Settings.Contracts;

public static class SettingsConstants
{
    public const string ModuleName = "Settings";
    public const string RoutePrefix = "/api/settings";
    public const string ViewPrefix = "/settings";

    public static class Routes
    {
        public static class Api
        {
            public const string GetSettings = "/";
            public const string GetSetting = "/{key}";
            public const string GetDefinitions = "/definitions";
            public const string UpdateSetting = "/";
            public const string DeleteSetting = "/{key}";
            public const string GetMySettings = "/me";
            public const string UpdateMySetting = "/me";
            public const string DeleteMySetting = "/me/{**key}";
            public const string GetMenuTree = "/menus";
            public const string GetAvailablePages = "/menus/available-pages";
            public const string CreateMenuItem = "/menus";
            public const string UpdateMenuItem = "/menus/{id}";
            public const string DeleteMenuItem = "/menus/{id}";
            public const string SetHomePage = "/menus/{id}/home";
            public const string ClearHomePage = "/menus/home";
            public const string ReorderMenuItems = "/menus/reorder";
        }

        public static class Views
        {
            public const string AdminSettings = "/";
            public const string UserSettings = "/me";
            public const string MenuManager = "/menus";
        }
    }
}
