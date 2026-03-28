using SimpleModule.Core;

namespace SimpleModule.FileStorage;

/// <summary>
/// Configurable options for the FileStorage module.
/// </summary>
public class FileStorageModuleOptions : IModuleOptions
{
    /// <summary>
    /// Maximum file size allowed for uploads, in megabytes. Default: 50.
    /// </summary>
    public int MaxFileSizeMb { get; set; } = 50;

    /// <summary>
    /// Comma-separated list of allowed file extensions for uploads.
    /// Default: ".jpg,.jpeg,.png,.gif,.pdf,.doc,.docx,.xls,.xlsx,.zip"
    /// </summary>
    public string AllowedExtensions { get; set; } =
        ".jpg,.jpeg,.png,.gif,.pdf,.doc,.docx,.xls,.xlsx,.zip";
}
