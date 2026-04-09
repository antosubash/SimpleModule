using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core;
using SimpleModule.Core.Events;
using SimpleModule.Email.Contracts;
using SimpleModule.Email.Contracts.Events;
using SimpleModule.Email.Jobs;
using SimpleModule.Email.Providers;
using SimpleModule.Email.Services;

namespace SimpleModule.Email;

public partial class EmailService(
    EmailDbContext db,
    IEmailProvider emailProvider,
    IEventBus eventBus,
    IBackgroundJobs backgroundJobs,
    ILogger<EmailService> logger
) : IEmailContracts
{
    public async Task<EmailMessage> SendEmailAsync(SendEmailRequest request)
    {
        var message = new EmailMessage
        {
            To = request.To,
            Cc = request.Cc,
            Bcc = request.Bcc,
            ReplyTo = request.ReplyTo,
            Subject = request.Subject,
            Body = request.Body,
            IsHtml = request.IsHtml,
            Status = EmailStatus.Queued,
            Provider = emailProvider.Name,
            CreatedAt = DateTime.UtcNow,
        };

        db.EmailMessages.Add(message);
        await db.SaveChangesAsync();

        await backgroundJobs.EnqueueAsync<SendEmailJob>(new SendEmailJobData(message.Id));

        LogEmailQueued(logger, message.Id, request.To);

        return message;
    }

    public async Task<EmailMessage> SendTemplatedEmailAsync(
        string templateSlug,
        string to,
        Dictionary<string, string> variables
    )
    {
        var template =
            await db.EmailTemplates.FirstOrDefaultAsync(t => t.Slug == templateSlug)
            ?? throw new Core.Exceptions.NotFoundException("EmailTemplate", templateSlug);

        var subjectVars = EmailTemplateRenderer.ExtractVariables(template.Subject);
        var bodyVars = EmailTemplateRenderer.ExtractVariables(template.Body);
        var allRequired = subjectVars.Union(bodyVars).ToHashSet();
        var missing = allRequired.Except(variables.Keys).ToList();

        if (missing.Count > 0)
        {
            throw new Core.Exceptions.ValidationException(
                new Dictionary<string, string[]>
                {
                    ["variables"] =
                    [
                        $"Template '{templateSlug}' requires variables: {string.Join(", ", missing)}",
                    ],
                }
            );
        }

        var renderedSubject = EmailTemplateRenderer.Render(
            template.Subject,
            variables,
            isHtml: false
        );
        var renderedBody = EmailTemplateRenderer.Render(template.Body, variables, template.IsHtml);

        return await SendEmailAsync(
            new SendEmailRequest
            {
                To = to,
                Subject = renderedSubject,
                Body = renderedBody,
                IsHtml = template.IsHtml,
                ReplyTo = template.DefaultReplyTo,
            }
        );
    }

    public async Task<PagedResult<EmailMessage>> QueryMessagesAsync(
        QueryEmailMessagesRequest request
    )
    {
        var query = db.EmailMessages.AsNoTracking().AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(m => m.Status == request.Status.Value);
        if (!string.IsNullOrWhiteSpace(request.To))
            query = query.Where(m => m.To.Contains(request.To));
        if (!string.IsNullOrWhiteSpace(request.Subject))
            query = query.Where(m => m.Subject.Contains(request.Subject));
        if (request.DateFrom.HasValue)
            query = query.Where(m => m.CreatedAt >= request.DateFrom.Value);
        if (request.DateTo.HasValue)
            query = query.Where(m => m.CreatedAt <= request.DateTo.Value);

        var totalCount = await query.CountAsync();

        var sortDescending = request.SortDescending ?? true;
        query = request.SortBy switch
        {
            "To" => sortDescending ? query.OrderByDescending(m => m.To) : query.OrderBy(m => m.To),
            "Subject" => sortDescending
                ? query.OrderByDescending(m => m.Subject)
                : query.OrderBy(m => m.Subject),
            "Status" => sortDescending
                ? query.OrderByDescending(m => m.Status)
                : query.OrderBy(m => m.Status),
            "CreatedAt" or _ => sortDescending
                ? query.OrderByDescending(m => m.CreatedAt)
                : query.OrderBy(m => m.CreatedAt),
        };

        var page = Math.Max(1, request.Page ?? 1);
        var pageSize = Math.Clamp(request.PageSize ?? 20, 1, 100);

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<EmailMessage>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<EmailMessage?> GetMessageByIdAsync(EmailMessageId id) =>
        await db.EmailMessages.FindAsync(id);

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
        var page = Math.Max(1, request.Page ?? 1);
        var pageSize = Math.Clamp(request.PageSize ?? 20, 1, 100);

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
            CreatedAt = DateTime.UtcNow,
        };

        db.EmailTemplates.Add(template);
        await db.SaveChangesAsync();

        LogTemplateCreated(logger, template.Id, template.Name);
        eventBus.PublishInBackground(
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
        template.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        LogTemplateUpdated(logger, template.Id, template.Name);
        eventBus.PublishInBackground(
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
        eventBus.PublishInBackground(new EmailTemplateDeletedEvent(id, templateName));
    }

    public async Task<EmailStats> GetEmailStatsAsync()
    {
        var now = DateTime.UtcNow;
        var last24Hours = now.AddHours(-24);
        var last7Days = now.AddDays(-7);
        var last30Days = now.AddDays(-30);

        var allMessages = db.EmailMessages.AsNoTracking();

        // Query 1: all-time status counts
        var statusCounts = await allMessages
            .GroupBy(m => m.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        // Query 2: top error messages
        var topErrors = await allMessages
            .Where(m => m.Status == EmailStatus.Failed && m.ErrorMessage != null)
            .GroupBy(m => m.ErrorMessage!)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new ErrorSummary { ErrorMessage = g.Key, Count = g.Count() })
            .ToListAsync();

        // Query 3: recent messages for time-windowed stats and daily volume
        // SQLite doesn't support Date grouping via EF Core; use client-side evaluation
        var recentMessages = await allMessages
            .Where(m => m.CreatedAt >= last30Days)
            .Select(m => new { m.CreatedAt, m.Status })
            .ToListAsync();

        // Derive 24h and 7d counts from the in-memory list
        var sent24h = recentMessages.Count(m =>
            m.Status == EmailStatus.Sent && m.CreatedAt >= last24Hours
        );
        var failed24h = recentMessages.Count(m =>
            m.Status == EmailStatus.Failed && m.CreatedAt >= last24Hours
        );
        var failed7d = recentMessages.Count(m =>
            m.Status == EmailStatus.Failed && m.CreatedAt >= last7Days
        );
        var total7d = recentMessages.Count(m => m.CreatedAt >= last7Days);
        var failureRate = total7d > 0 ? (double)failed7d / total7d * 100 : 0;

        var dailyVolume = recentMessages
            .GroupBy(m => m.CreatedAt.Date)
            .Select(g => new DailyCount
            {
                Date = g.Key,
                Sent = g.Count(m => m.Status == EmailStatus.Sent),
                Failed = g.Count(m => m.Status == EmailStatus.Failed),
            })
            .OrderBy(d => d.Date)
            .ToList();

        return new EmailStats
        {
            TotalSent = statusCounts.FirstOrDefault(s => s.Status == EmailStatus.Sent)?.Count ?? 0,
            TotalFailed =
                statusCounts.FirstOrDefault(s => s.Status == EmailStatus.Failed)?.Count ?? 0,
            TotalQueued =
                statusCounts.FirstOrDefault(s => s.Status == EmailStatus.Queued)?.Count ?? 0,
            TotalRetrying =
                statusCounts.FirstOrDefault(s => s.Status == EmailStatus.Retrying)?.Count ?? 0,
            SentLast24Hours = sent24h,
            FailedLast24Hours = failed24h,
            FailureRateLast7Days = Math.Round(failureRate, 2),
            TopErrors = topErrors,
            DailyVolume = dailyVolume,
        };
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Email {MessageId} queued for {To}")]
    private static partial void LogEmailQueued(ILogger logger, EmailMessageId messageId, string to);

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
