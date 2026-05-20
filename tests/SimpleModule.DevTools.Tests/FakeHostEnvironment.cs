using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace SimpleModule.DevTools.Tests;

internal sealed class FakeHostEnvironment(string contentRootPath) : IHostEnvironment
{
    public string ApplicationName { get; set; } = "TestApp";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = contentRootPath;
    public string EnvironmentName { get; set; } = "Development";
}
