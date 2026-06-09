using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using SimpleModule.OpenIddict.Contracts;
using SimpleModule.OpenIddict.Services;

namespace SimpleModule.OpenIddict.Tests.Unit;

public class OpenIddictProductionGuardTests
{
    private const string CertPath = "/certs/example.pfx";

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    public async Task StartAsync_LocalOrTest_DoesNotThrow_EvenWithUnsafeConfig(string environment)
    {
        // Password grant on + no certs would be refused in a real deployment,
        // but is tolerated locally.
        var guard = CreateGuard(environment, (ConfigKeys.OpenIddictAllowPasswordGrant, "true"));

        await guard.Invoking(g => g.StartAsync(default)).Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("QA")]
    public async Task StartAsync_RealDeployment_PasswordGrantEnabled_Throws(string environment)
    {
        var guard = CreateGuard(
            environment,
            (ConfigKeys.OpenIddictAllowPasswordGrant, "true"),
            (ConfigKeys.OpenIddictEncryptionCertPath, CertPath),
            (ConfigKeys.OpenIddictSigningCertPath, CertPath)
        );

        (
            await guard
                .Invoking(g => g.StartAsync(default))
                .Should()
                .ThrowAsync<InvalidOperationException>()
        ).WithMessage($"*{ConfigKeys.OpenIddictAllowPasswordGrant}*");
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task StartAsync_RealDeployment_MissingCertificates_Throws(string environment)
    {
        var guard = CreateGuard(environment); // no cert paths configured

        (
            await guard
                .Invoking(g => g.StartAsync(default))
                .Should()
                .ThrowAsync<InvalidOperationException>()
        ).WithMessage("*certificate*");
    }

    [Fact]
    public async Task StartAsync_RealDeployment_FullyConfigured_DoesNotThrow()
    {
        var guard = CreateGuard(
            "Production",
            (ConfigKeys.OpenIddictEncryptionCertPath, CertPath),
            (ConfigKeys.OpenIddictSigningCertPath, CertPath)
        );

        await guard.Invoking(g => g.StartAsync(default)).Should().NotThrowAsync();
    }

    private static OpenIddictProductionGuard CreateGuard(
        string environment,
        params (string Key, string Value)[] settings
    )
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(s => s.Key, s => (string?)s.Value))
            .Build();

        return new OpenIddictProductionGuard(configuration, new FakeHostEnvironment(environment));
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
