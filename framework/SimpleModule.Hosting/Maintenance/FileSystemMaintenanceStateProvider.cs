using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Maintenance;

namespace SimpleModule.Hosting.Maintenance;

/// <summary>
/// Reads the maintenance sentinel from the content root. The sentinel is a
/// JSON file written by <c>sm down</c> at deploy time; its absence means the
/// app is live. State is cached for <see cref="MaintenanceModeOptions.PollInterval"/>
/// to keep the per-request cost negligible.
/// </summary>
public sealed class FileSystemMaintenanceStateProvider : IMaintenanceStateProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _sentinelPath;
    private readonly TimeSpan _pollInterval;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<FileSystemMaintenanceStateProvider> _logger;

    private MaintenanceState? _cached;
    private DateTimeOffset _cachedAt = DateTimeOffset.MinValue;
    private readonly Lock _gate = new();

    public FileSystemMaintenanceStateProvider(
        IWebHostEnvironment environment,
        IOptions<MaintenanceModeOptions> options,
        TimeProvider timeProvider,
        ILogger<FileSystemMaintenanceStateProvider> logger
    )
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _sentinelPath = Path.Combine(environment.ContentRootPath, options.Value.SentinelFileName);
        _pollInterval = options.Value.PollInterval;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public ValueTask<MaintenanceState?> GetAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        lock (_gate)
        {
            if (now - _cachedAt < _pollInterval)
            {
                return ValueTask.FromResult(_cached);
            }
        }

        var fresh = ReadSentinel();

        lock (_gate)
        {
            _cached = fresh;
            _cachedAt = now;
        }

        return ValueTask.FromResult(fresh);
    }

    private MaintenanceState? ReadSentinel()
    {
        if (!File.Exists(_sentinelPath))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(_sentinelPath);
            var payload = JsonSerializer.Deserialize<SentinelPayload>(stream, JsonOptions);
            if (payload is null)
            {
                return new MaintenanceState { Active = true };
            }

            return new MaintenanceState
            {
                Active = true,
                Until = payload.Until,
                SecretHash = payload.SecretHash,
                Message = payload.Message,
                RetryAfterSeconds = payload.RetryAfterSeconds <= 0 ? 60 : payload.RetryAfterSeconds,
            };
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            _logger.LogWarning(
                ex,
                "Failed to read maintenance sentinel at {Path}; treating as active without metadata",
                _sentinelPath
            );
            return new MaintenanceState { Active = true };
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated by System.Text.Json via reflection."
    )]
    private sealed record SentinelPayload(
        DateTimeOffset? Until,
        string? SecretHash,
        string? Message,
        int RetryAfterSeconds
    );
}
