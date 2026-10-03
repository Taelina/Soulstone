using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Soulstone.SyncServer;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Soulstone.SyncServer.Tests;

internal static class TestNativeProvider
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        // Prefer the actual deployment library when supplied; otherwise exercise real
        // SQLCipher encryption with a test-only community binary, never plain SQLite.
        var providerAssembly = typeof(SQLitePCL.SQLite3Provider_sqlcipher).Assembly;
        NativeLibrary.SetDllImportResolver(providerAssembly, (name, assembly, searchPath) =>
        {
            if (name != "sqlcipher")
                return IntPtr.Zero;
            if (NativeLibrary.TryLoad(name, assembly, searchPath, out var native))
                return native;
            string filename = OperatingSystem.IsWindows() ? "e_sqlcipher.dll" :
                OperatingSystem.IsMacOS() ? "libe_sqlcipher.dylib" : "libe_sqlcipher.so";
            string path = Path.Combine(AppContext.BaseDirectory, "runtimes", RuntimeInformation.RuntimeIdentifier, "native", filename);
            return NativeLibrary.Load(File.Exists(path) ? path : filename, assembly, searchPath);
        });
    }
}

internal sealed class TestStorage : IDisposable
{
    private readonly string directory = Path.Combine(AppContext.BaseDirectory, "storage-tests", Guid.NewGuid().ToString("N"));
    public PublicationStorageOptions Options { get; }

    public TestStorage()
    {
        Directory.CreateDirectory(directory);
        Options = new PublicationStorageOptions
        {
            DatabasePath = Path.Combine(directory, "publications.db"),
            KeyFile = Path.Combine(directory, "secret.key"),
        };
        File.WriteAllText(Options.KeyFile, Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
    }

    public PublicationDatabase Open() => new(Options);

    public void Dispose()
    {
        // Only delete this helper's generated directory beneath the test output.
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "storage-tests")) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(directory).StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Test storage directory escaped the test output.");
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }
}

public sealed class RelayWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly TestStorage? storage;
    private readonly PublicationStorageOptions options;

    public RelayWebApplicationFactory()
    {
        storage = new TestStorage();
        options = storage.Options;
    }

    internal RelayWebApplicationFactory(PublicationStorageOptions options) => this.options = options;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Storage:DatabasePath", options.DatabasePath);
        builder.UseSetting("Storage:KeyFile", options.KeyFile);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            storage?.Dispose();
    }
}
