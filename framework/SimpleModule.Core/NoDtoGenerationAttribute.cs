using System;

namespace SimpleModule.Core;

/// <summary>
/// Excludes a public type in a Contracts assembly from automatic DTO/TypeScript generation.
/// By convention, all public types in *.Contracts assemblies are treated as DTOs.
/// Apply this attribute to types that should not be included.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface,
    AllowMultiple = false,
    Inherited = false
)]
public sealed class NoDtoGenerationAttribute : Attribute { }
