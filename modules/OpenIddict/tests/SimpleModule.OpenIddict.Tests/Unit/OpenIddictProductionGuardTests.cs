using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        // Password grant on + no certs is tolerated locally, without warnings —
        // ephemeral keys are the expected local setup.
        var (guard, logger) = CreateGuard(
            environment,
            (ConfigKeys.OpenIddictAllowPasswordGrant, "true")
        );

        await guard.Invoking(g => g.StartAsync(default)).Should().NotThrowAsync();
        logger.Entries.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("QA")]
    public async Task StartAsync_RealDeployment_PasswordGrantEnabled_Throws(string environment)
    {
        var (guard, _) = CreateGuard(
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
    public async Task StartAsync_RealDeployment_MissingCertificates_StartsAndLogsWarning(
        string environment
    )
    {
        var (guard, logger) = CreateGuard(environment); // no cert paths configured

        await guard.Invoking(g => g.StartAsync(default)).Should().NotThrowAsync();

        var warning = logger.Entries.Should().ContainSingle().Subject;
        warning.Level.Should().Be(LogLevel.Warning);
        warning.Message.Should().Contain(ConfigKeys.OpenIddictSigningCertPath);
        warning.Message.Should().Contain(ConfigKeys.OpenIddictEncryptionCertPath);
        warning.Message.Should().Contain("restart");
    }

    [Fact]
    public async Task StartAsync_RealDeployment_FullyConfigured_DoesNotThrowOrWarn()
    {
        var (guard, logger) = CreateGuard(
            "Production",
            (ConfigKeys.OpenIddictEncryptionCertPath, CertPath),
            (ConfigKeys.OpenIddictSigningCertPath, CertPath)
        );

        await guard.Invoking(g => g.StartAsync(default)).Should().NotThrowAsync();
        logger.Entries.Should().BeEmpty();
    }

    private static (OpenIddictProductionGuard Guard, CollectingLogger Logger) CreateGuard(
        string environment,
        params (string Key, string Value)[] settings
    )
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(s => s.Key, s => (string?)s.Value))
            .Build();

        var logger = new CollectingLogger();
        var guard = new OpenIddictProductionGuard(
            configuration,
            new FakeHostEnvironment(environment),
            logger
        );
        return (guard, logger);
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class CollectingLogger : ILogger<OpenIddictProductionGuard>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        ) => Entries.Add((logLevel, formatter(state, exception)));
    }
}
