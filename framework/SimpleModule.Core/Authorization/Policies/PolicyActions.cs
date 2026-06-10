namespace SimpleModule.Core.Authorization.Policies;

/// <summary>
/// Conventional action names for <see cref="IPolicy{TResource}"/> checks. Policies may
/// define additional module-specific actions; these constants just keep the common CRUD
/// verbs consistent across modules.
/// </summary>
public static class PolicyActions
{
    public const string View = "view";
    public const string Create = "create";
    public const string Update = "update";
    public const string Delete = "delete";
}
