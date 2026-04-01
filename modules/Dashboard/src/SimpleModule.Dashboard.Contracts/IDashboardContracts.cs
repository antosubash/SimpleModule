using System.Diagnostics.CodeAnalysis;

namespace SimpleModule.Dashboard.Contracts;

/// <summary>
/// Contract interface for the Dashboard module.
/// Other modules can depend on this to contribute dashboard widgets or data.
/// </summary>
[SuppressMessage(
    "Design",
    "CA1040:Avoid empty interfaces",
    Justification = "Contracts placeholder for future dashboard contributions"
)]
public interface IDashboardContracts { }
