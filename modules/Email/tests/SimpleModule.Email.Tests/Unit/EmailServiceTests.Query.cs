using FluentAssertions;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Tests.Unit;

public sealed partial class EmailServiceTests
{
    [Fact]
    public async Task QueryMessagesAsync_WithDefaults_ReturnsPaginatedResult()
    {
        for (var i = 0; i < 3; i++)
        {
            await _sut.SendEmailAsync(
                new SendEmailRequest
                {
                    To = $"user{i}@test.com",
                    Subject = $"Subject {i}",
                    Body = "Body",
                }
            );
        }

        var result = await _sut.QueryMessagesAsync(new QueryEmailMessagesRequest());

        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task QueryMessagesAsync_WithPageSize1_ReturnsOnlyFirstPage()
    {
        for (var i = 0; i < 3; i++)
        {
            await _sut.SendEmailAsync(
                new SendEmailRequest
                {
                    To = $"user{i}@test.com",
                    Subject = $"Subject {i}",
                    Body = "Body",
                }
            );
        }

        var result = await _sut.QueryMessagesAsync(new QueryEmailMessagesRequest { PageSize = 1 });

        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task QueryMessagesAsync_FilterByTo_ReturnsMatching()
    {
        await _sut.SendEmailAsync(
            new SendEmailRequest
            {
                To = "alice@test.com",
                Subject = "A",
                Body = "B",
            }
        );
        await _sut.SendEmailAsync(
            new SendEmailRequest
            {
                To = "bob@test.com",
                Subject = "A",
                Body = "B",
            }
        );

        var result = await _sut.QueryMessagesAsync(new QueryEmailMessagesRequest { To = "alice" });

        result.Items.Should().HaveCount(1);
        result.Items[0].To.Should().Be("alice@test.com");
    }

    [Fact]
    public async Task QueryTemplatesAsync_WithSearch_FiltersResults()
    {
        await _sut.CreateTemplateAsync(
            new CreateEmailTemplateRequest
            {
                Name = "Welcome",
                Slug = "welcome",
                Subject = "S",
                Body = "B",
            }
        );
        await _sut.CreateTemplateAsync(
            new CreateEmailTemplateRequest
            {
                Name = "Goodbye",
                Slug = "goodbye",
                Subject = "S",
                Body = "B",
            }
        );

        var result = await _sut.QueryTemplatesAsync(
            new QueryEmailTemplatesRequest { Search = "welcome" }
        );

        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Welcome");
    }

    [Fact]
    public async Task GetEmailStatsAsync_ReturnsCorrectCounts()
    {
        // Seed messages directly with specific statuses (not via SendEmailAsync which may change behavior later)
        _db.EmailMessages.AddRange(
            new EmailMessage
            {
                To = "a@test.com",
                Subject = "A",
                Body = "B",
                Status = EmailStatus.Sent,
                CreatedAt = DateTimeOffset.UtcNow,
                SentAt = DateTimeOffset.UtcNow,
            },
            new EmailMessage
            {
                To = "b@test.com",
                Subject = "B",
                Body = "B",
                Status = EmailStatus.Sent,
                CreatedAt = DateTimeOffset.UtcNow,
                SentAt = DateTimeOffset.UtcNow,
            },
            new EmailMessage
            {
                To = "c@test.com",
                Subject = "C",
                Body = "B",
                Status = EmailStatus.Failed,
                ErrorMessage = "Timeout",
                CreatedAt = DateTimeOffset.UtcNow,
            }
        );
        await _db.SaveChangesAsync();

        var stats = await _sut.GetEmailStatsAsync();

        stats.TotalSent.Should().Be(2);
        stats.TotalFailed.Should().Be(1);
        stats.SentLast24Hours.Should().Be(2);
        stats.FailedLast24Hours.Should().Be(1);
        stats.TopErrors.Should().HaveCount(1);
        stats.TopErrors[0].ErrorMessage.Should().Be("Timeout");
    }
}
