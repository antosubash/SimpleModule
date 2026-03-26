using SimpleModule.Core.Authorization;

namespace SimpleModule.FileStorage;

public sealed class FileStoragePermissions : IModulePermissions
{
    public const string View = "FileStorage.View";
    public const string Upload = "FileStorage.Upload";
    public const string Delete = "FileStorage.Delete";
}
