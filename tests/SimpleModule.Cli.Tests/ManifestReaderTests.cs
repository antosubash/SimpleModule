using System.IO.Compression;
using System.Reflection;
using System.Reflection.Emit;
using FluentAssertions;
using SimpleModule.Cli.Infrastructure;

namespace SimpleModule.Cli.Tests;

public sealed class ManifestReaderTests : IDisposable
{
    private const string SampleJson = """
        {"schemaVersion":1,"id":"SimpleModule.X","name":"X","displayName":"X Module","version":"1.0.0","frameworkCompat":">=0.0.38 <1.0.0","routePrefix":"/api/x","viewPrefix":"/x","schema":"X","permissions":["X.View"],"frontendEntry":"_content/SimpleModule.X/SimpleModule.X.pages.js","pages":["X/Browse"],"eventsPublished":[],"eventsConsumed":[],"hasDbContext":true}
        """;

    private readonly string _tempDir = Path.Combine(
        Path.GetTempPath(),
        "sm-manifest-tests-" + Guid.NewGuid().ToString("N")
    );

    public ManifestReaderTests() => Directory.CreateDirectory(_tempDir);

    [Fact]
    public void AssemblyManifestReader_ReadsManifestFromAttribute()
    {
        var dllPath = EmitAssemblyWithManifest("SimpleModule.X", SampleJson);

        var manifest = AssemblyManifestReader.TryRead(dllPath);

        manifest.Should().NotBeNull();
        manifest!.Id.Should().Be("SimpleModule.X");
        manifest.Name.Should().Be("X");
        manifest.FrameworkCompat.Should().Be(">=0.0.38 <1.0.0");
        manifest.Schema.Should().Be("X");
        manifest.HasDbContext.Should().BeTrue();
        manifest.FrontendEntry.Should().Be("_content/SimpleModule.X/SimpleModule.X.pages.js");
    }

    [Fact]
    public void AssemblyManifestReader_ReturnsNullForAssemblyWithoutManifest()
    {
        var dllPath = EmitAssemblyWithManifest("Plain.Assembly", manifestJson: null);

        AssemblyManifestReader.TryRead(dllPath).Should().BeNull();
    }

    [Fact]
    public void NupkgManifestReader_PrefersManifestJsonFile()
    {
        var nupkgPath = Path.Combine(_tempDir, "SimpleModule.X.1.0.0.nupkg");
        using (var zip = ZipFile.Open(nupkgPath, ZipArchiveMode.Create))
        {
            WriteEntry(zip, "module-manifest.json", SampleJson);
        }

        var manifest = NupkgManifestReader.TryRead(nupkgPath, "SimpleModule.X");

        manifest.Should().NotBeNull();
        manifest!.Id.Should().Be("SimpleModule.X");
    }

    [Fact]
    public void NupkgManifestReader_FallsBackToAssemblyAttribute()
    {
        var dllPath = EmitAssemblyWithManifest("SimpleModule.X", SampleJson);
        var nupkgPath = Path.Combine(_tempDir, "SimpleModule.X.1.0.1.nupkg");
        using (var zip = ZipFile.Open(nupkgPath, ZipArchiveMode.Create))
        {
            zip.CreateEntryFromFile(dllPath, "lib/net10.0/SimpleModule.X.dll");
        }

        var manifest = NupkgManifestReader.TryRead(nupkgPath, "SimpleModule.X");

        manifest.Should().NotBeNull();
        manifest!.Name.Should().Be("X");
    }

    [Fact]
    public void NupkgManifestReader_ReturnsNullForNonModulePackage()
    {
        var nupkgPath = Path.Combine(_tempDir, "Plain.Package.1.0.0.nupkg");
        using (var zip = ZipFile.Open(nupkgPath, ZipArchiveMode.Create))
        {
            WriteEntry(zip, "lib/net10.0/readme.txt", "hello");
        }

        NupkgManifestReader.TryRead(nupkgPath, "Plain.Package").Should().BeNull();
    }

    private static void WriteEntry(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }

    private static ConstructorInfo? _attributeCtor;

    /// <summary>
    /// Production module assemblies reference ModuleManifestAttribute from
    /// SimpleModule.Core, so the attribute constructor is a MemberReference to a
    /// TypeReference in another assembly. Recreate that exact shape: emit the
    /// attribute type into its own assembly once, load it, then reference its
    /// constructor from the test module assembly.
    /// </summary>
    private static ConstructorInfo GetAttributeCtor()
    {
        if (_attributeCtor is not null)
        {
            return _attributeCtor;
        }

        var builder = new PersistedAssemblyBuilder(
            new AssemblyName("SmTestAttributeAssembly"),
            typeof(object).Assembly
        );
        var module = builder.DefineDynamicModule("SmTestAttributeAssembly");
        var attrType = module.DefineType(
            "SimpleModule.Core.Modules.ModuleManifestAttribute",
            TypeAttributes.Public | TypeAttributes.Sealed,
            typeof(Attribute)
        );
        var ctor = attrType.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            [typeof(string)]
        );
        var il = ctor.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(
            OpCodes.Call,
            typeof(Attribute).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                Type.EmptyTypes
            )!
        );
        il.Emit(OpCodes.Ret);
        attrType.CreateType();

        var attrDllPath = Path.Combine(
            Path.GetTempPath(),
            "sm-test-attr-" + Guid.NewGuid().ToString("N") + ".dll"
        );
        builder.Save(attrDllPath);

        var loaded = Assembly.LoadFile(attrDllPath);
        _attributeCtor = loaded
            .GetType("SimpleModule.Core.Modules.ModuleManifestAttribute")!
            .GetConstructor([typeof(string)])!;
        return _attributeCtor;
    }

    private string EmitAssemblyWithManifest(string assemblyName, string? manifestJson)
    {
        var builder = new PersistedAssemblyBuilder(
            new AssemblyName(assemblyName),
            typeof(object).Assembly
        );
        var module = builder.DefineDynamicModule(assemblyName);
        // The module needs at least one type for a well-formed assembly.
        module.DefineType("Placeholder.Anchor", TypeAttributes.Public).CreateType();

        if (manifestJson is not null)
        {
            builder.SetCustomAttribute(
                new CustomAttributeBuilder(GetAttributeCtor(), [manifestJson])
            );
        }

        var dllPath = Path.Combine(_tempDir, assemblyName + ".dll");
        builder.Save(dllPath);
        return dllPath;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
