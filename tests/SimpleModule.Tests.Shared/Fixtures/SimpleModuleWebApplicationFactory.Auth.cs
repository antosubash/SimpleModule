using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Testing;
using SimpleModule.Testing;

namespace SimpleModule.Tests.Shared.Fixtures;

public partial class SimpleModuleWebApplicationFactory
{
    public HttpClient CreateAuthenticatedClient(
        string[] permissions,
        params Claim[] additionalClaims
    )
    {
        EnsureDatabasesInitialized();
        return WebApplicationFactoryAuthExtensions.CreateAuthenticatedClient(
            this,
            permissions,
            additionalClaims
        );
    }

    public HttpClient CreateAuthenticatedClient(params Claim[] claims)
    {
        EnsureDatabasesInitialized();
        return WebApplicationFactoryAuthExtensions.CreateAuthenticatedClient(this, claims);
    }

    public HttpClient CreateAuthenticatedClient(
        WebApplicationFactoryClientOptions clientOptions,
        params Claim[] claims
    )
    {
        EnsureDatabasesInitialized();
        return WebApplicationFactoryAuthExtensions.CreateAuthenticatedClient(
            this,
            clientOptions,
            claims
        );
    }
}
