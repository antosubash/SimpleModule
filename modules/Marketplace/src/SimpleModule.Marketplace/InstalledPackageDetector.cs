using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace SimpleModule.Marketplace;

public partial class InstalledPackageDetector(
    IWebHostEnvironment environment,
    IMemoryCache cache,
    ILogger<InstalledPackageDetector> logger
)
{
    private const string CacheKey = "Marketplace:InstalledPackages";

    public Task<HashSet<string>> GetInstalledPackageIdsAsync()
    {
        return Task.FromResult(
            cache.GetOrCreate(
                CacheKey,
                entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                    return ReadInstalledPackages();
                }
            ) ?? []
        );
    }

    private HashSet<string> ReadInstalledPackages()
    {
        var contentRoot = environment.ContentRootPath;
        var csprojFiles = Directory.GetFiles(
            contentRoot,
            "*.csproj",
            SearchOption.TopDirectoryOnly
        );

        var packages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var csproj in csprojFiles)
        {
            try
            {
                var doc = XDocument.Load(csproj);
                var packageRefs = doc.Descendants("PackageReference")
                    .Select(e => e.Attribute("Include")?.Value)
                    .Where(v => v is not null);

                foreach (var packageId in packageRefs)
                {
                    packages.Add(packageId!);
                }
            }
            catch (XmlException ex)
            {
                LogCsprojParseError(logger, csproj, ex);
            }
        }

        return packages;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to parse csproj file {CsprojPath}")]
    private static partial void LogCsprojParseError(
        ILogger logger,
        string csprojPath,
        XmlException exception
    );
}
