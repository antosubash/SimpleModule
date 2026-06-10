using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using SimpleModule.Core.Hosting;

namespace SimpleModule.Core.Tests.Hosting;

public class HostEnvironmentExtensionsTests
{
    [Theory]
    [InlineData("Development", true)]
    [InlineData("Testing", true)]
    [InlineData("Production", false)]
    [InlineData("Staging", false)]
    [InlineData("QA", false)]
    [InlineData("Local", false)]
    public void IsLocalOrTest_ClassifiesEnvironment(string environmentName, bool expected)
    {
        var environment = new FakeHostEnvironment(environmentName);

        environment.IsLocalOrTest().Should().Be(expected);
    }

    [Fact]
    public void IsLocalOrTest_Null_Throws()
    {
        var act = () => ((IHostEnvironment)null!).IsLocalOrTest();

        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
