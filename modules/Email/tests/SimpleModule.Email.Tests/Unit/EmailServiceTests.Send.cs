using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Email.Contracts;
using SimpleModule.Email.Jobs;

namespace SimpleModule.Email.Tests.Unit;

public sealed partial class EmailServiceTests
{
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
        result.Status.Should().Be(EmailStatus.Queued);
        result.SentAt.Should().BeNull();
        result.Id.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SendEmailAsync_EnqueuesBackgroundJob()
    {
        var request = new SendEmailRequest
        {
            To = "test@example.com",
            Subject = "Test",
            Body = "Body",
        };

        await _sut.SendEmailAsync(request);

        _backgroundJobs.EnqueuedJobs.Should().ContainSingle();
        _backgroundJobs.EnqueuedJobs[0].JobType.Should().Be<SendEmailJob>();
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
        result.Status.Should().Be(EmailStatus.Queued);
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
}
