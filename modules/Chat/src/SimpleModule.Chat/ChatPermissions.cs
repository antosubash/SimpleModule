using SimpleModule.Core.Authorization;

namespace SimpleModule.Chat;

public sealed class ChatPermissions : IModulePermissions
{
    public const string View = "Chat.View";
    public const string Create = "Chat.Create";
    public const string ManageAll = "Chat.ManageAll";
}
