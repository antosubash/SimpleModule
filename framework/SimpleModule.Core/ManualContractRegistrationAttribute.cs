namespace SimpleModule.Core;

/// <summary>
/// Marks a contract-interface implementation as registered manually by its owning
/// module — typically because the module wires it conditionally (e.g. one
/// implementation per configured provider) inside <c>ConfigureServices</c> rather
/// than relying on the source generator's auto-registration.
/// </summary>
/// <remarks>
/// <para>For a class carrying this attribute, the generator:</para>
/// <list type="bullet">
/// <item>does not auto-register it in the DI container (the module does so itself);</item>
/// <item>excludes it from <c>SM0026</c> "multiple implementations" — a contract may have
/// several provider-specific implementations that are chosen at runtime;</item>
/// <item>excludes it from <c>SM0028</c> "implementation must be public" — the class can stay
/// <c>internal</c> to its module since it is registered from within the same assembly.</item>
/// </list>
/// <para>The contract is still considered implemented, so <c>SM0025</c> "no implementation"
/// does not fire. Apply this to every implementation of a contract that the module
/// registers manually so the generator never tries to auto-wire any of them.</para>
/// <para>The attribute applies the same way to <c>IPolicy&lt;T&gt;</c> implementations:
/// a policy carrying it is not auto-registered, and the auto-registration rules
/// <c>SM0059</c> "policy must be public" and <c>SM0061</c> "policy must not be generic"
/// are waived — the module can register an internal or closed-generic policy itself.
/// The resource rules (<c>SM0058</c>, <c>SM0060</c>) still apply.</para>
/// </remarks>
/// <example>
/// <code>
/// [ManualContractRegistration]
/// internal sealed class ExternalUserService : IUserContracts { ... }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ManualContractRegistrationAttribute : Attribute;
