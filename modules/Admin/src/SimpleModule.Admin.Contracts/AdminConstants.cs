namespace SimpleModule.Admin.Contracts;

public static class AdminConstants
{
    public const string ModuleName = "Admin";
    public const string RoutePrefix = "/admin";
    public const string ViewPrefix = "/admin";

    public static class Routes
    {
        // View routes
        public const string Roles = "/roles";
        public const string RolesCreate = "/roles/create";
        public const string RolesEdit = "/roles/{id}/edit";
        public const string Users = "/users";
        public const string UsersCreate = "/users/create";
        public const string UsersEdit = "/users/{id}/edit";

        // API routes — AdminRolesEndpoint (group: /admin/roles)
        public const string RolesCreateApi = "/admin/roles";
        public const string RolesUpdateApi = "/admin/roles/{id}";
        public const string RolesPermissionsApi = "/admin/roles/{id}/permissions";
        public const string RolesDeleteApi = "/admin/roles/{id}";

        // API routes — AdminUsersEndpoint (group: /admin/users)
        public const string UsersCreateApi = "/admin/users";
        public const string UsersUpdateApi = "/admin/users/{id}";
        public const string UsersRolesApi = "/admin/users/{id}/roles";
        public const string UsersPermissionsApi = "/admin/users/{id}/permissions";
        public const string UsersResetPasswordApi = "/admin/users/{id}/reset-password";
        public const string UsersLockApi = "/admin/users/{id}/lock";
        public const string UsersUnlockApi = "/admin/users/{id}/unlock";
        public const string UsersForceReverifyApi = "/admin/users/{id}/force-reverify";
        public const string UsersDisable2faApi = "/admin/users/{id}/disable-2fa";
        public const string UsersDeactivateApi = "/admin/users/{id}/deactivate";
        public const string UsersReactivateApi = "/admin/users/{id}/reactivate";

        // API routes — AdminSessionsEndpoint (group: /admin/users/{id}/sessions)
        public const string SessionsRevokeApi = "/admin/users/{id}/sessions/{tokenId}";
        public const string SessionsRevokeAllApi = "/admin/users/{id}/sessions";
    }
}
