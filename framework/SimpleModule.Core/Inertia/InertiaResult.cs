using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SimpleModule.Core.Inertia;

public static class Inertia
{
    public static IResult Render(string component, object? props = null) =>
        new InertiaResult(component, props);
}

internal sealed class InertiaResult : IResult
{
    private static JsonSerializerOptions? _cachedOptions;

    private readonly string _component;
    private readonly object? _props;

    public InertiaResult(string component, object? props)
    {
        _component = component;
        _props = props;
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var options = GetSerializerOptions(httpContext);
        var sharedData = httpContext.RequestServices.GetService<InertiaSharedData>();
        var mergedProps = MergeProps(_props, sharedData, options);

        var pageData = new
        {
            component = _component,
            props = mergedProps,
            url = httpContext.Request.Path + httpContext.Request.QueryString,
            version = InertiaMiddleware.Version,
        };

        if (httpContext.Request.Headers.ContainsKey("X-Inertia"))
        {
            httpContext.Response.Headers["X-Inertia"] = "true";
            httpContext.Response.Headers["Vary"] = "X-Inertia";
            httpContext.Response.ContentType = "application/json";
            var json = JsonSerializer.Serialize(pageData, options);
            await httpContext.Response.WriteAsync(json);
            return;
        }

        var pageJson = JsonSerializer.Serialize(pageData, options);

        var renderer = httpContext.RequestServices.GetRequiredService<IInertiaPageRenderer>();
        await renderer.RenderPageAsync(httpContext, pageJson);
    }

    /// <summary>
    /// Resolves JSON serializer options from DI and merges with camelCase policy.
    /// Caches the merged options for subsequent requests since the DI options are
    /// configured once at startup and don't change.
    /// </summary>
    private static JsonSerializerOptions GetSerializerOptions(HttpContext httpContext)
    {
        if (_cachedOptions is not null)
            return _cachedOptions;

        var diOptions = httpContext.RequestServices.GetService<IOptions<JsonOptions>>();
        if (diOptions is not null)
        {
            var merged = new JsonSerializerOptions(diOptions.Value.SerializerOptions)
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
            _cachedOptions = merged;
            return merged;
        }

        var fallback = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        _cachedOptions = fallback;
        return fallback;
    }

    private static object MergeProps(
        object? props,
        InertiaSharedData? sharedData,
        JsonSerializerOptions options
    )
    {
        if (sharedData is null || sharedData.All.Count == 0)
        {
            return props ?? new { };
        }

        var result = new Dictionary<string, object?>();

        // Add shared data first (lower priority)
        foreach (var kvp in sharedData.All)
        {
            result[kvp.Key] = kvp.Value;
        }

        // Add endpoint props (higher priority — overwrites shared data)
        // Use JSON round-trip to merge endpoint props into shared data
        if (props is not null)
        {
            var json = JsonSerializer.SerializeToElement(props, options);
            foreach (var property in json.EnumerateObject())
            {
                result[property.Name] = property.Value;
            }
        }

        return result;
    }
}
