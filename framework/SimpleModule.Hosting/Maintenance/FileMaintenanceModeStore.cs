using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SimpleModule.Hosting.Maintenance;

/// <summary>
/// Sentinel-file backed maintenance store. The sentinel is a JSON document on disk,
/// watched by <see cref="FileSystemWatcher"/> so the running app picks up changes
/// the CLI makes without restart and without per-request disk I/O.
/// </summary>
public sealed class FileMaintenanceModeStore : IMaintenanceModeStore, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _sentinelPath;
    private readonly ILogger<FileMaintenanceModeStore> _logger;
    private readonly FileSystemWatcher? _watcher;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private MaintenanceModeState? _cached;

    public FileMaintenanceModeStore(
        IOptions<MaintenanceModeOptions> options,
        IHostEnvironment environment,
        ILogger<FileMaintenanceModeStore> logger
    )
    {
        _logger = logger;
        _sentinelPath =
            options.Value.SentinelPath
            ?? Path.Combine(environment.ContentRootPath, "App_Data", "maintenance.json");

        var dir = Path.GetDirectoryName(_sentinelPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
            _watcher = new FileSystemWatcher(dir, Path.GetFileName(_sentinelPath))
            {
                NotifyFilter =
                    NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
                EnableRaisingEvents = true,
            };
            _watcher.Changed += (_, _) => Reload();
            _watcher.Created += (_, _) => Reload();
            _watcher.Deleted += (_, _) => Reload();
            _watcher.Renamed += (_, _) => Reload();
        }

        Reload();
    }

    public MaintenanceModeState? GetState()
    {
        var state = _cached;
        if (state is null)
        {
            return null;
        }

        if (state.Until is { } until && DateTimeOffset.UtcNow >= until)
        {
            return null;
        }

        return state;
    }

    public async Task EnableAsync(
        MaintenanceModeState state,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(state);

        var dir = Path.GetDirectoryName(_sentinelPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(state, JsonOptions);

        // Serialize the actual write — concurrent callers shouldn't be able to produce
        // a torn file. SemaphoreSlim is used (not Monitor) because the body awaits.
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await File.WriteAllTextAsync(_sentinelPath, json, cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }

        _cached = state;
    }

    public async Task DisableAsync(CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(_sentinelPath))
            {
                File.Delete(_sentinelPath);
            }
        }
        finally
        {
            _writeLock.Release();
        }

        _cached = null;
    }

    private void Reload()
    {
        try
        {
            if (!File.Exists(_sentinelPath))
            {
                _cached = null;
                return;
            }

            // FileSystemWatcher fires before the writer closes the handle; brief retry
            // covers the gap without a sleep loop.
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    var json = File.ReadAllText(_sentinelPath);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        _cached = null;
                        return;
                    }

                    _cached = JsonSerializer.Deserialize<MaintenanceModeState>(json, JsonOptions);
                    return;
                }
                catch (IOException) when (attempt < 2)
                {
                    Thread.Sleep(25);
                }
            }
        }
        catch (IOException ex)
        {
            _logger.LogError(
                ex,
                "Failed to read maintenance sentinel at {Path}; treating as inactive",
                _sentinelPath
            );
            _cached = null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Maintenance sentinel at {Path} is not valid JSON; treating as inactive",
                _sentinelPath
            );
            _cached = null;
        }
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _writeLock.Dispose();
    }
}
