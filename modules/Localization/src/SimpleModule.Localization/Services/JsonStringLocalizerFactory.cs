using System.Collections.Concurrent;
using Microsoft.Extensions.Localization;

namespace SimpleModule.Localization.Services;

public sealed class JsonStringLocalizerFactory(TranslationLoader loader) : IStringLocalizerFactory
{
    private readonly ConcurrentDictionary<string, IStringLocalizer> _cache = new();

    public IStringLocalizer Create(Type resourceSource)
    {
        var ns = TranslationLoader.GetModuleName(resourceSource.Assembly)
            ?? resourceSource.Assembly.GetName().Name
            ?? "unknown";

        return _cache.GetOrAdd(ns, key => new JsonStringLocalizer(key, loader));
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        return _cache.GetOrAdd(baseName, key => new JsonStringLocalizer(key, loader));
    }
}
