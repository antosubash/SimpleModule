using SimpleModule.Identity.Contracts;

namespace SimpleModule.OpenIddict.Contracts;

/// <summary>
/// OpenIddict-specific session management contract. Inherits the provider-agnostic
/// <see cref="ISessionContracts"/> so consumers can depend on either interface.
/// </summary>
public interface IOpenIddictSessionContracts : ISessionContracts;
