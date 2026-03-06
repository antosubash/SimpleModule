using System;

namespace SimpleModule.Core;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct,
    AllowMultiple = false,
    Inherited = false
)]
public sealed class DtoAttribute : Attribute { }
