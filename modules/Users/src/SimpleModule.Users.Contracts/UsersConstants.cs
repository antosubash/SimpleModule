namespace SimpleModule.Users.Contracts;

public static class UsersConstants
{
    public const string ModuleName = "Users";
    public const string RoutePrefix = "/api/users";
    public const string ViewPrefix = "/Identity/Account";

    public static class Routes
    {
        // API endpoints (relative to RoutePrefix)
        public const string GetAll = "/";
        public const string Create = "/";
        public const string GetById = "/{id}";
        public const string Update = "/{id}";
        public const string Delete = "/{id}";
        public const string GetCurrent = "/me";
        public const string DownloadPersonalData = "/download-personal-data";

        // View endpoints (relative to ViewPrefix /Identity/Account)
        public const string Login = "/Login";
        public const string Register = "/Register";
        public const string ForgotPassword = "/ForgotPassword";
        public const string ForgotPasswordConfirmation = "/ForgotPasswordConfirmation";
        public const string ResetPassword = "/ResetPassword";
        public const string ResetPasswordConfirmation = "/ResetPasswordConfirmation";
        public const string ConfirmEmail = "/ConfirmEmail";
        public const string ConfirmEmailChange = "/ConfirmEmailChange";
        public const string ResendEmailConfirmation = "/ResendEmailConfirmation";
        public const string ExternalLogin = "/ExternalLogin";
        public const string Logout = "/Logout";
        public const string LoginWith2fa = "/LoginWith2fa";
        public const string LoginWithRecoveryCode = "/LoginWithRecoveryCode";
        public const string Lockout = "/Lockout";
        public const string AccessDenied = "/AccessDenied";
        public const string Error = "/Error";
        public const string RegisterConfirmation = "/RegisterConfirmation";

        // Manage routes (relative to ViewPrefix /Identity/Account)
        public const string ManageIndex = "/Manage";
        public const string ChangePassword = "/Manage/ChangePassword";
        public const string Email = "/Manage/Email";
        public const string SetPassword = "/Manage/SetPassword";
        public const string ExternalLogins = "/Manage/ExternalLogins";
        public const string DeletePersonalData = "/Manage/DeletePersonalData";
        public const string PersonalData = "/Manage/PersonalData";
        public const string TwoFactorAuthentication = "/Manage/TwoFactorAuthentication";
        public const string EnableAuthenticator = "/Manage/EnableAuthenticator";
        public const string Disable2fa = "/Manage/Disable2fa";
        public const string ResetAuthenticator = "/Manage/ResetAuthenticator";
        public const string GenerateRecoveryCodes = "/Manage/GenerateRecoveryCodes";
    }
}
