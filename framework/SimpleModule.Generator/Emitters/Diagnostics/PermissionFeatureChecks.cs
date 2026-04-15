using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SimpleModule.Generator;

internal static class PermissionFeatureChecks
{
    internal static void Run(SourceProductionContext context, DiscoveryData data)
    {
        // SM0027/SM0031/SM0032/SM0033/SM0034: Permission diagnostics
        // Track permission values for duplicate detection (value -> class FQN)
        var permissionValueOwners = new Dictionary<string, string>();

        foreach (var perm in data.PermissionClasses)
        {
            var permCleanName = Strip(perm.FullyQualifiedName);

            // SM0032: Not sealed
            if (!perm.IsSealed)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.PermissionClassNotSealed,
                        LocationHelper.ToLocation(perm.Location),
                        permCleanName
                    )
                );
            }

            foreach (var field in perm.Fields)
            {
                // SM0027: Field is not const string
                if (!field.IsConstString)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.PermissionFieldNotConstString,
                            LocationHelper.ToLocation(field.Location),
                            permCleanName,
                            field.FieldName
                        )
                    );
                    continue;
                }

                // SM0031: Value doesn't match {Module}.{Action} pattern (exactly one dot)
                var dotCount = 0;
                foreach (var ch in field.Value)
                {
                    if (ch == '.')
                        dotCount++;
                }
                if (dotCount != 1)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.PermissionValueBadPattern,
                            LocationHelper.ToLocation(field.Location),
                            field.Value,
                            permCleanName
                        )
                    );
                }

                // SM0034: Value prefix doesn't match module name
                if (dotCount >= 1)
                {
                    var prefix = field.Value.Substring(0, field.Value.IndexOf('.'));
                    if (!string.Equals(prefix, perm.ModuleName, System.StringComparison.Ordinal))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                DiagnosticDescriptors.PermissionValueWrongPrefix,
                                LocationHelper.ToLocation(field.Location),
                                field.Value,
                                perm.ModuleName
                            )
                        );
                    }
                }

                // SM0033: Duplicate permission value
                if (permissionValueOwners.TryGetValue(field.Value, out var existingOwner))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.DuplicatePermissionValue,
                            LocationHelper.ToLocation(field.Location),
                            field.Value,
                            existingOwner,
                            permCleanName
                        )
                    );
                }
                else
                {
                    permissionValueOwners[field.Value] = permCleanName;
                }
            }
        }

        // SM0044: Multiple IModuleOptions for same module
        var optionsByModule = ModuleOptionsRecord.GroupByModule(data.ModuleOptions);

        foreach (var kvp in optionsByModule)
        {
            if (kvp.Value.Count > 1)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.MultipleModuleOptions,
                        LocationHelper.ToLocation(kvp.Value[1].Location),
                        kvp.Key,
                        Strip(kvp.Value[0].FullyQualifiedName),
                        Strip(kvp.Value[1].FullyQualifiedName)
                    )
                );
            }
        }

        // SM0045/SM0046/SM0047/SM0048: Feature flag diagnostics
        var featureValueOwners = new Dictionary<string, string>();

        foreach (var feat in data.FeatureClasses)
        {
            var featCleanName = Strip(feat.FullyQualifiedName);

            // SM0045: Not sealed
            if (!feat.IsSealed)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.FeatureClassNotSealed,
                        LocationHelper.ToLocation(feat.Location),
                        featCleanName
                    )
                );
            }

            foreach (var field in feat.Fields)
            {
                // SM0048: Not a const string
                if (!field.IsConstString)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.FeatureFieldNotConstString,
                            LocationHelper.ToLocation(field.Location),
                            field.FieldName,
                            featCleanName
                        )
                    );
                    continue;
                }

                // SM0046: Naming violation
                if (!field.Value.Contains("."))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.FeatureFieldNamingViolation,
                            LocationHelper.ToLocation(field.Location),
                            field.Value,
                            featCleanName
                        )
                    );
                }

                // SM0047: Duplicate feature name
                if (featureValueOwners.TryGetValue(field.Value, out var existingOwner))
                {
                    if (existingOwner != feat.FullyQualifiedName)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                DiagnosticDescriptors.DuplicateFeatureName,
                                LocationHelper.ToLocation(field.Location),
                                field.Value,
                                Strip(existingOwner),
                                featCleanName
                            )
                        );
                    }
                }
                else
                {
                    featureValueOwners[field.Value] = feat.FullyQualifiedName;
                }
            }
        }
    }

    private static string Strip(string fqn) => TypeMappingHelpers.StripGlobalPrefix(fqn);
}
