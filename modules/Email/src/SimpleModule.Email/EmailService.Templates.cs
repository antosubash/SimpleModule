using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.Core;
using SimpleModule.Email.Contracts;
using SimpleModule.Email.Contracts.Events;

namespace SimpleModule.Email;

public partial class EmailService
{
    public async Task<PagedResult<EmailTemplate>> QueryTemplatesAsync(
        QueryEmailTemplatesRequest request
    )
    {
        var query = db.EmailTemplates.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(t =>
                t.Name.Contains(request.Search) || t.Slug.Contains(request.Search)
            );

        var totalCount = await query.CountAsync();
        var page = request.EffectivePage;
        var pageSize = request.EffectivePageSize;

        var items = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<EmailTemplate>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<EmailTemplate?> GetTemplateByIdAsync(EmailTemplateId id) =>
        await db.EmailTemplates.FindAsync(id);

    public async Task<EmailTemplate?> GetTemplateBySlugAsync(string slug) =>
        await db.EmailTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Slug == slug);

    public async Task<EmailTemplate> CreateTemplateAsync(CreateEmailTemplateRequest request)
    {
        var slugExists = await db.EmailTemplates.AnyAsync(t => t.Slug == request.Slug);
        if (slugExists)
        {
            throw new Core.Exceptions.ConflictException(
                $"A template with slug '{request.Slug}' already exists."
            );
        }

        var template = new EmailTemplate
        {
            Name = request.Name,
            Slug = request.Slug,
            Subject = request.Subject,
            Body = request.Body,
            IsHtml = request.IsHtml,
            DefaultReplyTo = request.DefaultReplyTo,
        };

        db.EmailTemplates.Add(template);
        await db.SaveChangesAsync();

        LogTemplateCreated(logger, template.Id, template.Name);
        await bus.PublishAsync(
            new EmailTemplateCreatedEvent(template.Id, template.Name, template.Slug)
        );

        return template;
    }

    public async Task<EmailTemplate> UpdateTemplateAsync(
        EmailTemplateId id,
        UpdateEmailTemplateRequest request
    )
    {
        var template =
            await db.EmailTemplates.FindAsync(id)
            ?? throw new Core.Exceptions.NotFoundException("EmailTemplate", id);

        var changedFields = new List<string>();
        if (template.Name != request.Name)
            changedFields.Add("Name");
        if (template.Subject != request.Subject)
            changedFields.Add("Subject");
        if (template.Body != request.Body)
            changedFields.Add("Body");
        if (template.IsHtml != request.IsHtml)
            changedFields.Add("IsHtml");
        if (template.DefaultReplyTo != request.DefaultReplyTo)
            changedFields.Add("DefaultReplyTo");

        template.Name = request.Name;
        template.Subject = request.Subject;
        template.Body = request.Body;
        template.IsHtml = request.IsHtml;
        template.DefaultReplyTo = request.DefaultReplyTo;

        await db.SaveChangesAsync();

        LogTemplateUpdated(logger, template.Id, template.Name);
        await bus.PublishAsync(
            new EmailTemplateUpdatedEvent(template.Id, template.Name, changedFields)
        );

        return template;
    }

    public async Task DeleteTemplateAsync(EmailTemplateId id)
    {
        var template =
            await db.EmailTemplates.FindAsync(id)
            ?? throw new Core.Exceptions.NotFoundException("EmailTemplate", id);

        var templateName = template.Name;
        db.EmailTemplates.Remove(template);
        await db.SaveChangesAsync();

        LogTemplateDeleted(logger, id);
        await bus.PublishAsync(new EmailTemplateDeletedEvent(id, templateName));
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Email template {TemplateId} created: {TemplateName}"
    )]
    private static partial void LogTemplateCreated(
        ILogger logger,
        EmailTemplateId templateId,
        string templateName
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Email template {TemplateId} updated: {TemplateName}"
    )]
    private static partial void LogTemplateUpdated(
        ILogger logger,
        EmailTemplateId templateId,
        string templateName
    );

    [LoggerMessage(Level = LogLevel.Information, Message = "Email template {TemplateId} deleted")]
    private static partial void LogTemplateDeleted(ILogger logger, EmailTemplateId templateId);
}
