using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleModule.Core;
using SimpleModule.Core.Events;
using SimpleModule.Email.Contracts;
using SimpleModule.Email.Contracts.Events;
using SimpleModule.Email.Providers;
using SimpleModule.Email.Services;

namespace SimpleModule.Email;

public partial class EmailService(
    EmailDbContext db,
    IEmailProvider emailProvider,
    IOptions<EmailModuleOptions> options,
    IEventBus eventBus,
    ILogger<EmailService> logger
) : IEmailContracts
{
    public async Task<EmailMessage> SendEmailAsync(SendEmailRequest request)
    {
        var opts = options.Value;
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

        try
        {
            var envelope = new EmailEnvelope(
                opts.DefaultFromAddress,
                opts.DefaultFromName,
                request.To,
                request.Cc,
                request.Bcc,
                request.ReplyTo,
                request.Subject,
                request.Body,
                request.IsHtml
            );

            await emailProvider.SendAsync(envelope);

            message.Status = EmailStatus.Sent;
            message.SentAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            LogEmailSent(logger, message.Id, request.To);
            eventBus.PublishInBackground(
                new EmailSentEvent(message.Id, request.To, request.Subject)
            );
        }
        catch (InvalidOperationException ex)
        {
            await HandleSendFailure(message, request, ex);
        }
        catch (System.Net.Sockets.SocketException ex)
        {
            await HandleSendFailure(message, request, ex);
        }
        catch (IOException ex)
        {
            await HandleSendFailure(message, request, ex);
        }

        return message;
    }

    private async Task HandleSendFailure(
        EmailMessage message,
        SendEmailRequest request,
        Exception ex
    )
    {
        message.Status = EmailStatus.Failed;
        message.ErrorMessage = ex.Message;
        await db.SaveChangesAsync();

        LogEmailFailed(logger, message.Id, request.To, ex);
        eventBus.PublishInBackground(
            new EmailFailedEvent(message.Id, request.To, request.Subject, ex.Message)
        );
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
            template.IsHtml
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

    public Task<PagedResult<EmailMessage>> QueryMessagesAsync(QueryEmailMessagesRequest request) =>
        throw new NotImplementedException();

    public async Task<EmailMessage?> GetMessageByIdAsync(EmailMessageId id) =>
        await db.EmailMessages.FindAsync(id);

    public Task<PagedResult<EmailTemplate>> QueryTemplatesAsync(
        QueryEmailTemplatesRequest request
    ) => throw new NotImplementedException();

    public async Task<EmailTemplate?> GetTemplateByIdAsync(EmailTemplateId id) =>
        await db.EmailTemplates.FindAsync(id);

    public async Task<EmailTemplate?> GetTemplateBySlugAsync(string slug) =>
        await db.EmailTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Slug == slug);

    public async Task<EmailTemplate> CreateTemplateAsync(CreateEmailTemplateRequest request)
    {
        var template = new EmailTemplate
        {
            Name = request.Name,
            Slug = request.Slug,
            Subject = request.Subject,
            Body = request.Body,
            IsHtml = request.IsHtml,
            CreatedAt = DateTime.UtcNow,
        };

        db.EmailTemplates.Add(template);
        await db.SaveChangesAsync();

        LogTemplateCreated(logger, template.Id, template.Name);

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

        template.Name = request.Name;
        template.Subject = request.Subject;
        template.Body = request.Body;
        template.IsHtml = request.IsHtml;
        template.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        LogTemplateUpdated(logger, template.Id, template.Name);

        return template;
    }

    public async Task DeleteTemplateAsync(EmailTemplateId id)
    {
        var template =
            await db.EmailTemplates.FindAsync(id)
            ?? throw new Core.Exceptions.NotFoundException("EmailTemplate", id);

        db.EmailTemplates.Remove(template);
        await db.SaveChangesAsync();

        LogTemplateDeleted(logger, id);
    }

    public Task<EmailStats> GetEmailStatsAsync() => throw new NotImplementedException();

    [LoggerMessage(Level = LogLevel.Information, Message = "Email {MessageId} sent to {To}")]
    private static partial void LogEmailSent(ILogger logger, EmailMessageId messageId, string to);

    [LoggerMessage(Level = LogLevel.Error, Message = "Email {MessageId} failed to send to {To}")]
    private static partial void LogEmailFailed(
        ILogger logger,
        EmailMessageId messageId,
        string to,
        Exception ex
    );

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
