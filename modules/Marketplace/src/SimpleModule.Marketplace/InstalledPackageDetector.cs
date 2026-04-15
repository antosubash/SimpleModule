using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;

namespace SimpleModule.Marketplace;

public partial class InstalledPackageDetector(
    IWebHostEnvironment environment,
    IFusionCache cache,
    ILogger<InstalledPackageDetector> logger
)
{
    private const string CacheKey = "Marketplace:InstalledPackages";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

    public async Task<HashSet<string>> GetInstalledPackageIdsAsync()
    {
        var result = await cache.GetOrSetAsync<HashSet<string>>(
            CacheKey,
            (_, _) => Task.FromResult(ReadInstalledPackages()),
            opts => opts.Duration = CacheDuration
        );
        return result ?? [];
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
                var xmlSettings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                };
                using var xmlReader = XmlReader.Create(csproj, xmlSettings);
                var doc = XDocument.Load(xmlReader);
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
