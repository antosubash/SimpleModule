using System.Buffers;
using System.Linq;
using System.Text;
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
    private static volatile JsonSerializerOptions? _cachedOptions;

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
        var url = httpContext.Request.Path + httpContext.Request.QueryString;
        var pageJson = SerializePage(_component, _props, sharedData, url, options);

        if (httpContext.Request.IsInertia())
        {
            httpContext.Response.Headers[InertiaHttpExtensions.InertiaHeader] = "true";
            httpContext.Response.Headers["Vary"] = InertiaHttpExtensions.InertiaHeader;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(pageJson);
            return;
        }

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

    /// <summary>
    /// Serializes the Inertia page envelope in a single pass. When there is no shared
    /// data, endpoint props stream straight to the output with no intermediate DOM.
    /// When shared data is present, endpoint props must be materialized once (to know
    /// their keys), then shared data and props are written into one object — endpoint
    /// props keep priority (shared keys they define are skipped), so no key is emitted
    /// twice.
    /// </summary>
    private static string SerializePage(
        string component,
        object? props,
        InertiaSharedData? sharedData,
        string url,
        JsonSerializerOptions options
    )
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("component", component);

            writer.WritePropertyName("props");
            if (sharedData is null || sharedData.All.Count == 0)
            {
                JsonSerializer.Serialize(writer, props ?? EmptyProps, options);
            }
            else
            {
                WriteMergedProps(writer, props, sharedData, options);
            }

            writer.WriteString("url", url);
            writer.WriteString("version", InertiaMiddleware.Version);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void WriteMergedProps(
        Utf8JsonWriter writer,
        object? props,
        InertiaSharedData sharedData,
        JsonSerializerOptions options
    )
    {
        // Endpoint props are materialized once so their top-level keys are known;
        // this is unavoidable for a correct merge but happens a single time.
        using var propsDoc =
            props is null ? null : JsonSerializer.SerializeToDocument(props, options);

        var propKeys =
            propsDoc is null
                ? null
                : new HashSet<string>(
                    propsDoc.RootElement.EnumerateObject().Select(p => p.Name),
                    StringComparer.Ordinal
                );

        writer.WriteStartObject();

        // Shared data (lower priority) — skip any key the endpoint props also define.
        foreach (var kvp in sharedData.All)
        {
            if (propKeys is not null && propKeys.Contains(kvp.Key))
                continue;

            writer.WritePropertyName(kvp.Key);
            JsonSerializer.Serialize(writer, kvp.Value, options);
        }

        // Endpoint props (higher priority).
        if (propsDoc is not null)
        {
            foreach (var property in propsDoc.RootElement.EnumerateObject())
            {
                property.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
    }

    private static readonly object EmptyProps = new();
}
