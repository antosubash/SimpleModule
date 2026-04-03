using SimpleModule.Core.Authorization;

namespace SimpleModule.Email;

public sealed class EmailPermissions : IModulePermissions
{
    public const string ViewTemplates = "Email.ViewTemplates";
    public const string ManageTemplates = "Email.ManageTemplates";
    public const string ViewHistory = "Email.ViewHistory";
    public const string Send = "Email.Send";
}
