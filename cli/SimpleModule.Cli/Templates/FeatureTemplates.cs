using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Templates;

public sealed class FeatureTemplates
{
    private readonly SolutionContext _solution;
    private readonly string? _refModule;
    private readonly string? _refSingular;

    public FeatureTemplates(SolutionContext solution)
    {
        _solution = solution;
        _refModule = solution.ExistingModules.Count > 0 ? solution.ExistingModules[0] : null;
        _refSingular = _refModule is not null ? ModuleTemplates.GetSingularName(_refModule) : null;
    }

    public string Endpoint(
        string moduleName,
        string featureName,
        string httpMethod,
        string route,
        string singularName
    )
    {
        // Try to read the reference endpoint and adapt it
        if (_refModule is not null)
        {
            var refPath = Path.Combine(
                _solution.GetModuleProjectPath(_refModule),
                "Endpoints",
                _refModule,
                "GetAllEndpoint.cs"
            );

            if (File.Exists(refPath))
            {
                return AdaptEndpointFromReference(
                    refPath,
                    moduleName,
                    featureName,
                    httpMethod,
                    route,
                    singularName
                );
            }
        }

        return FallbackEndpoint(moduleName, featureName, httpMethod, route, singularName);
    }

    public string Validator(string moduleName, string featureName, string singularName)
    {
        // Try to read from an existing validator in the reference module
        if (_refModule is not null)
        {
            var refValidators = Directory.GetFiles(
                _solution.GetModuleProjectPath(_refModule),
                "*Validator.cs",
                SearchOption.AllDirectories
            );

            if (refValidators.Length > 0)
            {
                return AdaptValidatorFromReference(
                    refValidators[0],
                    moduleName,
                    featureName,
                    singularName
                );
            }
        }

        return FallbackValidator(moduleName, featureName, singularName);
    }

    private string AdaptEndpointFromReference(
        string refPath,
        string moduleName,
        string featureName,
        string httpMethod,
        string route,
        string singularName
    )
    {
        // Read the reference endpoint
        var content = File.ReadAllText(refPath);

        // Replace module/feature names
        content = TemplateExtractor.ReplaceModuleNames(
            content,
            _refModule!,
            _refSingular!,
            moduleName,
            singularName
        );

        // Replace the endpoint namespace
        content = content.Replace(
            $"Endpoints.{moduleName}",
            $"Endpoints.{moduleName}",
            StringComparison.Ordinal
        );

        // Replace the class name
        content = content.Replace(
            "GetAllEndpoint",
            $"{featureName}Endpoint",
            StringComparison.Ordinal
        );

        // Replace HTTP method
        var mapMethod = httpMethod.ToUpperInvariant() switch
        {
            "POST" => "MapPost",
            "PUT" => "MapPut",
            "DELETE" => "MapDelete",
            _ => "MapGet",
        };
        content = content.Replace("MapGet", mapMethod, StringComparison.Ordinal);

        // Replace route
        content = content.Replace("\"/" + "\"", $"\"{route}\"", StringComparison.Ordinal);

        // For non-GET methods, adjust return type
        if (!string.Equals(httpMethod, "GET", StringComparison.OrdinalIgnoreCase))
        {
            // Replace the lambda body to be generic
            var returnStatement = httpMethod.ToUpperInvariant() switch
            {
                "POST" => "return TypedResults.Created();",
                "DELETE" => "return TypedResults.NoContent();",
                "PUT" => "return TypedResults.NoContent();",
                _ => "return TypedResults.Ok();",
            };

            // Replace everything between the lambda braces with a TODO + return
            var lines = content.Split(["\r\n", "\n"], StringSplitOptions.None).ToList();
            var inLambda = false;
            var lambdaBraceDepth = 0;
            var newLines = new List<string>();

            foreach (var line in lines)
            {
                if (
                    !inLambda
                    && line.Contains($"(I{singularName}Contracts", StringComparison.Ordinal)
                )
                {
                    inLambda = true;
                    lambdaBraceDepth = 0;
                    newLines.Add(line);
                    continue;
                }

                if (inLambda)
                {
                    if (line.TrimStart().StartsWith('{') && lambdaBraceDepth == 0)
                    {
                        lambdaBraceDepth = 1;
                        newLines.Add(line);
                        newLines.Add($"                        // TODO: implement");
                        newLines.Add($"                        {returnStatement}");
                        continue;
                    }

                    if (lambdaBraceDepth > 0)
                    {
                        lambdaBraceDepth += line.Count(c => c == '{') - line.Count(c => c == '}');
                        if (lambdaBraceDepth <= 0)
                        {
                            inLambda = false;
                            newLines.Add(line);
                        }

                        continue;
                    }

                    newLines.Add(line);
                }
                else
                {
                    newLines.Add(line);
                }
            }

            content = string.Join(Environment.NewLine, newLines);
        }

        // Fix route: replace "\"/\"" with the actual route
        // The reference has "/" as route, we need to replace it
        if (route != "/")
        {
            content = content.Replace("\"/\"", $"\"{route}\"", StringComparison.Ordinal);
        }

        return content;
    }

    private string AdaptValidatorFromReference(
        string refPath,
        string moduleName,
        string featureName,
        string singularName
    )
    {
        var content = File.ReadAllText(refPath);

        // Replace module names
        content = TemplateExtractor.ReplaceModuleNames(
            content,
            _refModule!,
            _refSingular!,
            moduleName,
            singularName
        );

        // Replace the feature-specific class name and namespace
        var refFileName = Path.GetFileNameWithoutExtension(refPath);
        content = content.Replace(
            refFileName,
            $"{featureName}RequestValidator",
            StringComparison.Ordinal
        );

        // Replace the feature namespace
        var lines = content.Split(["\r\n", "\n"], StringSplitOptions.None).ToList();
        for (var i = 0; i < lines.Count; i++)
        {
            if (
                lines[i].Contains("namespace ", StringComparison.Ordinal)
                && lines[i].Contains(".Endpoints.", StringComparison.Ordinal)
            )
            {
                // Extract the namespace prefix and replace the endpoint namespace
                var nsPrefix = lines[i][
                    ..lines[i].IndexOf(".Endpoints.", StringComparison.Ordinal)
                ];
                var moduleSuffix = lines[i].Contains(';', StringComparison.Ordinal)
                    ? lines[i][
                        (
                            lines[i].IndexOf(".Endpoints.", StringComparison.Ordinal)
                            + ".Endpoints.".Length
                        )..lines[i].IndexOf(';', StringComparison.Ordinal)
                    ]
                    : moduleName;
                lines[i] = $"{nsPrefix}.Endpoints.{moduleSuffix};";
            }
        }

        content = string.Join(Environment.NewLine, lines);

        // Simplify: replace specific validation logic with TODO
        // Find method body and replace with generic content
        var resultLines = content.Split(["\r\n", "\n"], StringSplitOptions.None).ToList();
        var methodStart = resultLines.FindIndex(l =>
            l.Contains("Validate(", StringComparison.Ordinal)
        );
        if (methodStart >= 0)
        {
            // Find the opening brace after the method declaration
            var braceStart = resultLines.FindIndex(methodStart, l => l.TrimStart().StartsWith('{'));
            if (braceStart >= 0)
            {
                // Find matching closing brace
                var depth = 1;
                var braceEnd = braceStart + 1;
                while (braceEnd < resultLines.Count && depth > 0)
                {
                    depth +=
                        resultLines[braceEnd].Count(c => c == '{')
                        - resultLines[braceEnd].Count(c => c == '}');
                    braceEnd++;
                }

                // Replace method body
                var newBody = new List<string>
                {
                    resultLines[braceStart], // opening brace
                    "        var errors = new Dictionary<string, string[]>();",
                    "",
                    "        // TODO: add validation rules",
                    "",
                    "        return errors.Count > 0 ? ValidationResult.WithErrors(errors) : ValidationResult.Success;",
                };

                // Keep the closing brace
                if (braceEnd - 1 < resultLines.Count)
                {
                    newBody.Add(resultLines[braceEnd - 1]);
                }

                resultLines.RemoveRange(braceStart, braceEnd - braceStart);
                resultLines.InsertRange(braceStart, newBody);
            }
        }

        return string.Join(Environment.NewLine, resultLines);
    }

    private static string FallbackEndpoint(
        string moduleName,
        string featureName,
        string httpMethod,
        string route,
        string singularName
    )
    {
        var mapMethod = httpMethod.ToUpperInvariant() switch
        {
            "POST" => "MapPost",
            "PUT" => "MapPut",
            "DELETE" => "MapDelete",
            _ => "MapGet",
        };

        var returnStatement = httpMethod.ToUpperInvariant() switch
        {
            "POST" => "return TypedResults.Created();",
            "PUT" => "return TypedResults.NoContent();",
            "DELETE" => "return TypedResults.NoContent();",
            _ => "return TypedResults.Ok();",
        };

        return $$"""
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Routing;
            using SimpleModule.Core;
            using SimpleModule.{{moduleName}}.Contracts;

            namespace SimpleModule.{{moduleName}}.Endpoints.{{moduleName}};

            public class {{featureName}}Endpoint : IEndpoint
            {
                public void Map(IEndpointRouteBuilder app)
                {
                    app.{{mapMethod}}(
                        "{{route}}",
                        async (I{{singularName}}Contracts contracts) =>
                        {
                            // TODO: implement
                            {{returnStatement}}
                        }
                    );
                }
            }
            """;
    }

    public static string ViewComponent(string moduleName, string featureName) =>
        $$"""
        type Props = {
            // TODO: add props from your endpoint's response
        }

        export default function {{featureName}}({ }: Props) {
            return (
                <div>
                    <h1>{{featureName}}</h1>
                </div>
            )
        }
        """;

    private static string FallbackValidator(
        string moduleName,
        string featureName,
        string singularName
    ) =>
        $$"""
            using SimpleModule.Core.Validation;
            using SimpleModule.{{moduleName}}.Contracts;

            namespace SimpleModule.{{moduleName}}.Endpoints.{{moduleName}};

            public static class {{featureName}}RequestValidator
            {
                public static ValidationResult Validate({{singularName}} request)
                {
                    var errors = new Dictionary<string, string[]>();

                    // TODO: add validation rules

                    return errors.Count > 0 ? ValidationResult.WithErrors(errors) : ValidationResult.Success;
                }
            }
            """;
}
