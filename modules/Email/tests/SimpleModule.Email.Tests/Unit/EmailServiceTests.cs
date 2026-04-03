using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Events;
using SimpleModule.Database;
using SimpleModule.Email.Contracts;
using SimpleModule.Email.Providers;

namespace SimpleModule.Email.Tests.Unit;

public sealed class EmailServiceTests : IDisposable
{
    private readonly EmailDbContext _db;
    private readonly EmailService _sut;
    private readonly TestEventBus _eventBus = new();

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

        var emailOptions = Options.Create(new EmailModuleOptions());
        var provider = new LogEmailProvider(NullLogger<LogEmailProvider>.Instance);

        _sut = new EmailService(
            _db,
            provider,
            emailOptions,
            _eventBus,
            NullLogger<EmailService>.Instance
        );
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task SendEmailAsync_CreatesMessageAndSends()
    {
        var request = new SendEmailRequest
        {
            To = "test@example.com",
            Subject = "Test Subject",
            Body = "Test Body",
            IsHtml = false,
        };

        var result = await _sut.SendEmailAsync(request);

        result.Should().NotBeNull();
        result.To.Should().Be("test@example.com");
        result.Subject.Should().Be("Test Subject");
        result.Status.Should().Be(EmailStatus.Sent);
        result.SentAt.Should().NotBeNull();
        result.Id.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SendEmailAsync_PublishesEmailSentEvent()
    {
        var request = new SendEmailRequest
        {
            To = "test@example.com",
            Subject = "Test",
            Body = "Body",
        };

        await _sut.SendEmailAsync(request);

        _eventBus.PublishedEvents.Should().ContainSingle();
    }

    [Fact]
    public async Task SendEmailAsync_PersistsMessages()
    {
        await _sut.SendEmailAsync(
            new SendEmailRequest
            {
                To = "a@test.com",
                Subject = "A",
                Body = "A",
            }
        );
        await _sut.SendEmailAsync(
            new SendEmailRequest
            {
                To = "b@test.com",
                Subject = "B",
                Body = "B",
            }
        );

        var messages = await _db.EmailMessages.AsNoTracking().ToListAsync();

        messages.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMessageByIdAsync_WithExistingId_ReturnsMessage()
    {
        var sent = await _sut.SendEmailAsync(
            new SendEmailRequest
            {
                To = "a@test.com",
                Subject = "A",
                Body = "A",
            }
        );

        var found = await _sut.GetMessageByIdAsync(sent.Id);

        found.Should().NotBeNull();
        found!.To.Should().Be("a@test.com");
    }

    [Fact]
    public async Task CreateTemplateAsync_CreatesAndReturnsTemplate()
    {
        var request = new CreateEmailTemplateRequest
        {
            Name = "Welcome Email",
            Slug = "welcome",
            Subject = "Welcome {{name}}",
            Body = "<h1>Hello {{name}}</h1>",
            IsHtml = true,
        };

        var result = await _sut.CreateTemplateAsync(request);

        result.Should().NotBeNull();
        result.Name.Should().Be("Welcome Email");
        result.Slug.Should().Be("welcome");
        result.Id.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateTemplateAsync_PersistsTemplates()
    {
        await _sut.CreateTemplateAsync(
            new CreateEmailTemplateRequest
            {
                Name = "T1",
                Slug = "t1",
                Subject = "S1",
                Body = "B1",
            }
        );
        await _sut.CreateTemplateAsync(
            new CreateEmailTemplateRequest
            {
                Name = "T2",
                Slug = "t2",
                Subject = "S2",
                Body = "B2",
            }
        );

        var templates = await _db.EmailTemplates.AsNoTracking().ToListAsync();

        templates.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateTemplateAsync_UpdatesExistingTemplate()
    {
        var created = await _sut.CreateTemplateAsync(
            new CreateEmailTemplateRequest
            {
                Name = "Original",
                Slug = "original",
                Subject = "Original Subject",
                Body = "Original Body",
            }
        );

        var updated = await _sut.UpdateTemplateAsync(
            created.Id,
            new UpdateEmailTemplateRequest
            {
                Name = "Updated",
                Subject = "Updated Subject",
                Body = "Updated Body",
                IsHtml = true,
            }
        );

        updated.Name.Should().Be("Updated");
        updated.Subject.Should().Be("Updated Subject");
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateTemplateAsync_WithNonExistentId_ThrowsNotFoundException()
    {
        var act = () =>
            _sut.UpdateTemplateAsync(
                EmailTemplateId.From(99999),
                new UpdateEmailTemplateRequest
                {
                    Name = "Test",
                    Subject = "Test",
                    Body = "Test",
                }
            );

        await act.Should()
            .ThrowAsync<Core.Exceptions.NotFoundException>()
            .WithMessage("*EmailTemplate*99999*not found*");
    }

    [Fact]
    public async Task DeleteTemplateAsync_RemovesTemplate()
    {
        var created = await _sut.CreateTemplateAsync(
            new CreateEmailTemplateRequest
            {
                Name = "ToDelete",
                Slug = "to-delete",
                Subject = "S",
                Body = "B",
            }
        );

        await _sut.DeleteTemplateAsync(created.Id);

        var found = await _sut.GetTemplateByIdAsync(created.Id);
        found.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTemplateAsync_WithNonExistentId_ThrowsNotFoundException()
    {
        var act = () => _sut.DeleteTemplateAsync(EmailTemplateId.From(99999));

        await act.Should()
            .ThrowAsync<Core.Exceptions.NotFoundException>()
            .WithMessage("*EmailTemplate*99999*not found*");
    }

    [Fact]
    public async Task SendTemplatedEmailAsync_RendersTemplateAndSends()
    {
        await _sut.CreateTemplateAsync(
            new CreateEmailTemplateRequest
            {
                Name = "Welcome",
                Slug = "welcome",
                Subject = "Welcome {{name}}",
                Body = "<p>Hello {{name}}, welcome to {{app}}!</p>",
                IsHtml = true,
            }
        );

        var result = await _sut.SendTemplatedEmailAsync(
            "welcome",
            "user@test.com",
            new Dictionary<string, string> { ["name"] = "John", ["app"] = "SimpleModule" }
        );

        result.Should().NotBeNull();
        result.To.Should().Be("user@test.com");
        result.Subject.Should().Be("Welcome John");
        result.Body.Should().Contain("Hello John");
        result.Body.Should().Contain("welcome to SimpleModule!");
        result.Status.Should().Be(EmailStatus.Sent);
    }

    [Fact]
    public async Task SendTemplatedEmailAsync_WithNonExistentSlug_ThrowsNotFoundException()
    {
        var act = () =>
            _sut.SendTemplatedEmailAsync(
                "nonexistent",
                "user@test.com",
                new Dictionary<string, string>()
            );

        await act.Should().ThrowAsync<Core.Exceptions.NotFoundException>();
    }

    [Fact]
    public async Task GetTemplateBySlugAsync_ReturnsCorrectTemplate()
    {
        await _sut.CreateTemplateAsync(
            new CreateEmailTemplateRequest
            {
                Name = "Test",
                Slug = "test-slug",
                Subject = "S",
                Body = "B",
            }
        );

        var found = await _sut.GetTemplateBySlugAsync("test-slug");

        found.Should().NotBeNull();
        found!.Slug.Should().Be("test-slug");
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
