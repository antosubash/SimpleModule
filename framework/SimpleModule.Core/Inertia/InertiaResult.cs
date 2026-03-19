using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleModule.Core.Inertia;

public static class Inertia
{
    public static IResult Render(string component, object? props = null) =>
        new InertiaResult(component, props);
}

internal sealed class InertiaResult : IResult
{
    private static readonly JsonSerializerOptions _camelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _component;
    private readonly object? _props;

    public InertiaResult(string component, object? props)
    {
        _component = component;
        _props = props;
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var sharedData = httpContext.RequestServices.GetService<InertiaSharedData>();
        var mergedProps = MergeProps(_props, sharedData);

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
            await httpContext.Response.WriteAsJsonAsync(pageData);
            return;
        }

        var pageJson = JsonSerializer.Serialize(pageData, _camelCaseOptions);

        var renderer = httpContext.RequestServices.GetRequiredService<IInertiaPageRenderer>();
        await renderer.RenderPageAsync(httpContext, pageJson);
    }

    private static object MergeProps(object? props, InertiaSharedData? sharedData)
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
            var json = JsonSerializer.SerializeToElement(props, _camelCaseOptions);
            foreach (var property in json.EnumerateObject())
            {
                result[property.Name] = property.Value;
            }
        }

        return result;
    }
}
