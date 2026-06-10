namespace SimpleModule.Core.Authorization.Policies;

/// <summary>
/// Thrown when an authorization check runs for a resource type that has no registered
/// <see cref="IPolicy{TResource}"/>. This is a developer error (fail closed, loudly) —
/// add a policy class for the resource or remove the check.
/// </summary>
public sealed class MissingPolicyException : InvalidOperationException
{
    public MissingPolicyException()
        : base("No policy is registered for the requested resource type.") { }

    public MissingPolicyException(string message)
        : base(message) { }

    public MissingPolicyException(string message, Exception innerException)
        : base(message, innerException) { }

    public static MissingPolicyException ForResource(Type resourceType) =>
        new(
            $"No IPolicy<{resourceType?.Name}> is registered. "
                + "Add a policy class implementing IPolicy<T> in the module that owns the resource, "
                + "or remove the authorization check."
        );
}
