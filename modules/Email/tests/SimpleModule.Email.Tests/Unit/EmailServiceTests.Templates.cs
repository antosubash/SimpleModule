using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Tests.Unit;

public sealed partial class EmailServiceTests
{
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
}
