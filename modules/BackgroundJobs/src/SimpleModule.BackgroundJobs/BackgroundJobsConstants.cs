namespace SimpleModule.BackgroundJobs;

public static class BackgroundJobsConstants
{
    public const string ModuleName = "BackgroundJobs";
    public const string RoutePrefix = "/api/jobs";
    public const string DispatcherFunctionName = "ModuleJobDispatcher";
    public const string UnknownValue = "Unknown";

    /// <summary>
    /// Extracts the short class name from an assembly-qualified type name.
    /// E.g. "Namespace.MyJob, Assembly, ..." → "MyJob"
    /// </summary>
    public static string GetShortTypeName(string? assemblyQualifiedName)
    {
        if (string.IsNullOrEmpty(assemblyQualifiedName))
        {
            return UnknownValue;
        }

        return assemblyQualifiedName.Split(',')[0].Split('.').Last();
    }

    /// <summary>
    /// Derives the module name from an assembly-qualified type name.
    /// E.g. "Ns.MyJob, SimpleModule.Products, ..." → "Products"
    /// </summary>
    public static string GetModuleName(string? assemblyQualifiedName)
    {
        if (string.IsNullOrEmpty(assemblyQualifiedName))
        {
            return UnknownValue;
        }

        var assemblyPart = assemblyQualifiedName.Split(',').ElementAtOrDefault(1)?.Trim();
        return assemblyPart?.Replace("SimpleModule.", "", StringComparison.Ordinal) ?? UnknownValue;
    }

    public static DateTimeOffset AsUtc(DateTime dt) =>
        new(DateTime.SpecifyKind(dt, DateTimeKind.Utc), TimeSpan.Zero);
}
