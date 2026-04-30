using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleModule.Testing;

public static class TestAuthExtensions
{
    /// <summary>
    /// Registers the <see cref="TestAuthHandler"/> under
    /// <see cref="TestAuthDefaults.AuthenticationScheme"/> and makes it the
    /// default authenticate/challenge scheme. Intended for use inside
    /// <c>WebApplicationFactory.ConfigureWebHost</c>.
    /// </summary>
    public static AuthenticationBuilder AddTestAuthentication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthDefaults.AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthDefaults.AuthenticationScheme,
                _ => { }
            );
    }
}
