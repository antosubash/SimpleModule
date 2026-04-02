using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Localization;
using SimpleModule.Core;

namespace SimpleModule.Localization.Services;

public sealed class JsonStringLocalizerFactory(TranslationLoader loader) : IStringLocalizerFactory
{
    private readonly TranslationLoader _loader = loader;
    private readonly ConcurrentDictionary<string, IStringLocalizer> _cache = new();

    public IStringLocalizer Create(Type resourceSource)
    {
        var moduleAttr = resourceSource.GetCustomAttribute<ModuleAttribute>()
            ?? resourceSource.Assembly.GetCustomAttributes<ModuleAttribute>().FirstOrDefault();

        var ns = moduleAttr?.Name ?? resourceSource.Assembly.GetName().Name ?? "unknown";

        return _cache.GetOrAdd(ns, key => new JsonStringLocalizer(key, _loader));
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        return _cache.GetOrAdd(baseName, key => new JsonStringLocalizer(key, _loader));
    }
}
