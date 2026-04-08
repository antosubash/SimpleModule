using SimpleModule.Core.Authorization;

namespace SimpleModule.Datasets.Contracts;

public sealed class DatasetsPermissions : IModulePermissions
{
    public const string View = "Datasets.View";
    public const string Upload = "Datasets.Upload";
    public const string Convert = "Datasets.Convert";
    public const string Delete = "Datasets.Delete";
}
