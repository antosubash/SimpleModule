using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SimpleModule.Notifications.Contracts;
using SimpleModule.Notifications.Infrastructure;
using SimpleModule.Notifications.Infrastructure.Channels;
using SimpleModule.Notifications.Infrastructure.Jobs;
using SimpleModule.Users.Contracts;
using Wolverine;

namespace SimpleModule.Notifications.Tests.Unit;

public sealed class NotifierTests
{
    private sealed class CapturingChannel(string name) : INotificationChannel
    {
        public string Name { get; } = name;
        public List<(NotificationRecipient Recipient, INotification Notification)> Calls { get; } =
        [];

        public Task SendAsync(
            NotificationRecipient recipient,
            INotification notification,
            CancellationToken cancellationToken = default
        )
        {
            Calls.Add((recipient, notification));
            return Task.CompletedTask;
        }
    }

    private sealed class FailingChannel(string name) : INotificationChannel
    {
        public string Name { get; } = name;

        public Task SendAsync(
            NotificationRecipient recipient,
            INotification notification,
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("boom");
    }

    private sealed record TestNotification(string[] Channels) : INotification
    {
        public string NotificationType => "test.event";

        public string[] Via(NotificationRecipient recipient) => Channels;

        public DatabaseNotificationPayload? ToDatabase(NotificationRecipient recipient) =>
            new("Title", "Body");
    }

    [Fact]
    public async Task SendNowAsync_DispatchesToEachChannel()
    {
        var db = new CapturingChannel(NotificationsConstants.Channels.Database);
        var mail = new CapturingChannel(NotificationsConstants.Channels.Mail);
        var registry = new NotificationChannelRegistry([db, mail]);
        var sut = new Notifier(
            registry,
            new TestBackgroundJobs(),
            Substitute.For<IMessageBus>(),
            NullLogger<Notifier>.Instance
        );

        var recipient = new NotificationRecipient(UserId.From("u1"), "u1@test.com");
        var notification = new TestNotification([
            NotificationsConstants.Channels.Database,
            NotificationsConstants.Channels.Mail,
        ]);

        await sut.SendNowAsync(recipient, notification);

        db.Calls.Should().HaveCount(1);
        mail.Calls.Should().HaveCount(1);
    }

    [Fact]
    public async Task SendNowAsync_UnknownChannelIsSkipped()
    {
        var db = new CapturingChannel(NotificationsConstants.Channels.Database);
        var registry = new NotificationChannelRegistry([db]);
        var sut = new Notifier(
            registry,
            new TestBackgroundJobs(),
            Substitute.For<IMessageBus>(),
            NullLogger<Notifier>.Instance
        );

        var recipient = new NotificationRecipient(UserId.From("u1"));
        const string unregisteredChannel = "unregistered";
        var notification = new TestNotification([
            unregisteredChannel,
            NotificationsConstants.Channels.Database,
        ]);

        await sut.SendNowAsync(recipient, notification);

        db.Calls.Should().HaveCount(1);
    }

    [Fact]
    public async Task SendNowAsync_PublishesFailedEvent_WhenChannelThrows()
    {
        var failing = new FailingChannel(NotificationsConstants.Channels.Database);
        var registry = new NotificationChannelRegistry([failing]);
        var bus = Substitute.For<IMessageBus>();
        var sut = new Notifier(
            registry,
            new TestBackgroundJobs(),
            bus,
            NullLogger<Notifier>.Instance
        );

        var recipient = new NotificationRecipient(UserId.From("u1"));
        var notification = new TestNotification([NotificationsConstants.Channels.Database]);

        await sut.SendNowAsync(recipient, notification);

        await bus.Received(1)
            .PublishAsync(
                Arg.Any<Contracts.Events.NotificationFailedEvent>(),
                Arg.Any<DeliveryOptions?>()
            );
    }

    [Fact]
    public async Task SendAsync_EnqueuesOneJobPerChannel()
    {
        var registry = new NotificationChannelRegistry([]);
        var jobs = new TestBackgroundJobs();
        var sut = new Notifier(
            registry,
            jobs,
            Substitute.For<IMessageBus>(),
            NullLogger<Notifier>.Instance
        );

        var recipient = new NotificationRecipient(UserId.From("u1"), "u1@test.com");
        var notification = new TestNotification([
            NotificationsConstants.Channels.Database,
            NotificationsConstants.Channels.Mail,
        ]);

        await sut.SendAsync(recipient, notification);

        jobs.EnqueuedJobs.Should().HaveCount(2);
        jobs.EnqueuedJobs.Should()
            .AllSatisfy(j => j.JobType.Should().Be<DispatchNotificationJob>());
    }
}
