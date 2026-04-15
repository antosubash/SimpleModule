using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core.Events;
using SimpleModule.Database;
using SimpleModule.Email.Providers;

namespace SimpleModule.Email.Tests.Unit;

public sealed partial class EmailServiceTests : IDisposable
{
    private readonly EmailDbContext _db;
    private readonly EmailService _sut;
    private readonly TestEventBus _eventBus = new();
    private readonly TestBackgroundJobs _backgroundJobs = new();

    public EmailServiceTests()
    {
        var options = new DbContextOptionsBuilder<EmailDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var dbOptions = Options.Create(
            new DatabaseOptions
            {
                ModuleConnections = new Dictionary<string, string>
                {
                    ["Email"] = "Data Source=:memory:",
                },
            }
        );
        _db = new EmailDbContext(options, dbOptions);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();

        var provider = new LogEmailProvider(NullLogger<LogEmailProvider>.Instance);

        _sut = new EmailService(
            _db,
            provider,
            _eventBus,
            _backgroundJobs,
            NullLogger<EmailService>.Instance
        );
    }

    public void Dispose() => _db.Dispose();

    private sealed class TestBackgroundJobs : IBackgroundJobs
    {
        public List<(Type JobType, object? Data)> EnqueuedJobs { get; } = [];

        public Task<JobId> EnqueueAsync<TJob>(object? data = null, CancellationToken ct = default)
            where TJob : IModuleJob
        {
            EnqueuedJobs.Add((typeof(TJob), data));
            return Task.FromResult(JobId.From(Guid.NewGuid()));
        }

        public Task<JobId> ScheduleAsync<TJob>(
            DateTimeOffset executeAt,
            object? data = null,
            CancellationToken ct = default
        )
            where TJob : IModuleJob => Task.FromResult(JobId.From(Guid.NewGuid()));

        public Task<RecurringJobId> AddRecurringAsync<TJob>(
            string name,
            string cronExpression,
            object? data = null,
            CancellationToken ct = default
        )
            where TJob : IModuleJob => Task.FromResult(RecurringJobId.From(Guid.NewGuid()));

        public Task RemoveRecurringAsync(RecurringJobId id, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<bool> ToggleRecurringAsync(RecurringJobId id, CancellationToken ct = default) =>
            Task.FromResult(true);

        public Task CancelAsync(JobId jobId, CancellationToken ct = default) => Task.CompletedTask;

        public Task<JobStatusDto?> GetStatusAsync(JobId jobId, CancellationToken ct = default) =>
            Task.FromResult<JobStatusDto?>(null);
    }

    private sealed class TestEventBus : IEventBus
    {
        public List<IEvent> PublishedEvents { get; } = [];

        public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
            where T : IEvent
        {
            PublishedEvents.Add(@event);
            return Task.CompletedTask;
        }

        public void PublishInBackground<T>(T @event)
            where T : IEvent
        {
            PublishedEvents.Add(@event);
        }
    }
}
