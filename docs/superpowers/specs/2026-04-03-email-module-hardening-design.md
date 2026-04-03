# Email Module Production Hardening

**Date:** 2026-04-03
**Status:** Draft
**Module:** `SimpleModule.Email`

## Overview

The Email module is currently at MVP level — it sends emails synchronously, stores unbounded history, and lacks validation, retry logic, and observability. This design hardens the module for medium-volume production use (hundreds of emails/day) by adding pagination, validation, background sending with retry, audit logging, template safety, and a statistics dashboard.

## Goals

- Handle hundreds of emails per day without memory or latency issues
- Never lose a failed email — retry automatically up to a configurable limit
- Catch invalid input at the API boundary before it reaches the service layer
- Provide a compliance-grade audit trail for all email operations
- Prevent silent template rendering bugs and HTML injection
- Give admins visibility into email health via a dashboard

## Non-Goals

- Bulk/marketing email sends (batch endpoints, unsubscribe management)
- Email attachments (deferred to a future iteration)
- Multi-tenant email isolation (deferred until tenant-aware email routing is needed)
- Custom email provider plugins (SMTP and Log remain the two providers)

## New Dependency

`SimpleModule.BackgroundJobs.Contracts` — for `IBackgroundJobs.EnqueueAsync` and `AddRecurringAsync`. This is a contracts-only dependency (no implementation coupling).

---

## 1. Pagination & Filtering

### Problem

`GetAllMessagesAsync` and `GetAllTemplatesAsync` load entire tables into memory. Email messages accumulate unboundedly.

### Design

**New DTOs in `Email.Contracts`:**

```csharp
[Dto]
public class QueryEmailMessagesRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public EmailStatus? Status { get; set; }
    public string? To { get; set; }
    public string? Subject { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

[Dto]
public class QueryEmailTemplatesRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
}
```

**Contract changes:**

```csharp
public interface IEmailContracts
{
    // New paginated queries
    Task<PagedResult<EmailMessage>> QueryMessagesAsync(QueryEmailMessagesRequest request);
    Task<PagedResult<EmailTemplate>> QueryTemplatesAsync(QueryEmailTemplatesRequest request);

    // Existing methods retained
    Task<EmailMessage> SendEmailAsync(SendEmailRequest request);
    Task<EmailMessage> SendTemplatedEmailAsync(string templateSlug, string to, Dictionary<string, string> variables);
    Task<EmailMessage?> GetMessageByIdAsync(EmailMessageId id);
    Task<EmailTemplate?> GetTemplateByIdAsync(EmailTemplateId id);
    Task<EmailTemplate?> GetTemplateBySlugAsync(string slug);
    Task<EmailTemplate> CreateTemplateAsync(CreateEmailTemplateRequest request);
    Task<EmailTemplate> UpdateTemplateAsync(EmailTemplateId id, UpdateEmailTemplateRequest request);
    Task DeleteTemplateAsync(EmailTemplateId id);
    Task<EmailStats> GetEmailStatsAsync();
}
```

The existing `GetAllMessagesAsync` and `GetAllTemplatesAsync` are removed from `IEmailContracts`. They become `internal` methods on `EmailService` for use by background jobs (retry job needs to query failed messages).

**Filtering implementation:** `EmailService.QueryMessagesAsync` builds an `IQueryable<EmailMessage>` with conditional `Where` clauses, applies sorting via a `switch` on `SortBy`, then calls the framework's `ToPagedResultAsync` extension.

**View endpoints:** `HistoryEndpoint` and `TemplatesEndpoint` accept `[AsParameters] QueryEmailMessagesRequest` / `QueryEmailTemplatesRequest` and pass `PagedResult<T>` as props. The React views receive `{ data, page, pageSize, totalCount }` and pass `data` to `DataGridPage`.

---

## 2. Request Validation

### Problem

Endpoints accept raw requests without validation. Invalid email addresses, empty slugs, and oversized fields reach the database.

### Design

**Static validator classes** using `ValidationBuilder.AddErrorIf()` (the only method on `ValidationBuilder`, matching the Products/Orders/Tenants pattern):

```csharp
public static class SendEmailRequestValidator
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public static ValidationResult Validate(SendEmailRequest request) =>
        new ValidationBuilder()
            .AddErrorIf(string.IsNullOrWhiteSpace(request.To), "To", "Recipient is required.")
            .AddErrorIf(!string.IsNullOrWhiteSpace(request.To) && !EmailRegex.IsMatch(request.To), "To", "Invalid email format.")
            .AddErrorIf(!string.IsNullOrWhiteSpace(request.ReplyTo) && !EmailRegex.IsMatch(request.ReplyTo), "ReplyTo", "Invalid email format.")
            .AddErrorIf(string.IsNullOrWhiteSpace(request.Subject), "Subject", "Subject is required.")
            .AddErrorIf(request.Subject?.Length > 500, "Subject", "Subject must not exceed 500 characters.")
            .AddErrorIf(string.IsNullOrWhiteSpace(request.Body), "Body", "Body is required.")
            .Build();
}

public static class CreateEmailTemplateRequestValidator
{
    private static readonly Regex SlugRegex = new(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public static ValidationResult Validate(CreateEmailTemplateRequest request) =>
        new ValidationBuilder()
            .AddErrorIf(string.IsNullOrWhiteSpace(request.Name), "Name", "Name is required.")
            .AddErrorIf(request.Name?.Length > 200, "Name", "Name must not exceed 200 characters.")
            .AddErrorIf(string.IsNullOrWhiteSpace(request.Slug), "Slug", "Slug is required.")
            .AddErrorIf(request.Slug?.Length > 200, "Slug", "Slug must not exceed 200 characters.")
            .AddErrorIf(!string.IsNullOrWhiteSpace(request.Slug) && !SlugRegex.IsMatch(request.Slug), "Slug", "Slug must be lowercase alphanumeric with hyphens.")
            .AddErrorIf(string.IsNullOrWhiteSpace(request.Subject), "Subject", "Subject is required.")
            .AddErrorIf(request.Subject?.Length > 500, "Subject", "Subject must not exceed 500 characters.")
            .AddErrorIf(string.IsNullOrWhiteSpace(request.Body), "Body", "Body is required.")
            .AddErrorIf(!string.IsNullOrWhiteSpace(request.DefaultReplyTo) && !EmailRegex.IsMatch(request.DefaultReplyTo), "DefaultReplyTo", "Invalid email format.")
            .Build();
}

public static class UpdateEmailTemplateRequestValidator
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public static ValidationResult Validate(UpdateEmailTemplateRequest request) =>
        new ValidationBuilder()
            .AddErrorIf(string.IsNullOrWhiteSpace(request.Name), "Name", "Name is required.")
            .AddErrorIf(request.Name?.Length > 200, "Name", "Name must not exceed 200 characters.")
            .AddErrorIf(string.IsNullOrWhiteSpace(request.Subject), "Subject", "Subject is required.")
            .AddErrorIf(request.Subject?.Length > 500, "Subject", "Subject must not exceed 500 characters.")
            .AddErrorIf(string.IsNullOrWhiteSpace(request.Body), "Body", "Body is required.")
            .AddErrorIf(!string.IsNullOrWhiteSpace(request.DefaultReplyTo) && !EmailRegex.IsMatch(request.DefaultReplyTo), "DefaultReplyTo", "Invalid email format.")
            .Build();
}
```

**Slug uniqueness** is checked in `EmailService.CreateTemplateAsync` — queries DB for existing slug, throws `ConflictException` if taken. This is the only validation that requires DB access.

**Endpoint integration:** Each endpoint calls the validator before the service. On failure, returns `TypedResults.ValidationProblem(result.Errors)`.

---

## 3. ReplyTo Support & EmailEnvelope

### Problem

No ReplyTo support. The `IEmailProvider.SendAsync` method has 8+ positional parameters (parameter sprawl).

### Design

**New fields:**

| Location | Field | Type |
|---|---|---|
| `SendEmailRequest` | `ReplyTo` | `string?` |
| `EmailMessage` entity | `ReplyTo` | `string?`, max 500 chars |
| `CreateEmailTemplateRequest` | `DefaultReplyTo` | `string?` |
| `UpdateEmailTemplateRequest` | `DefaultReplyTo` | `string?` |
| `EmailTemplate` entity | `DefaultReplyTo` | `string?`, max 500 chars |

**EmailEnvelope parameter object** replaces the 8+ params on `IEmailProvider`:

```csharp
public sealed record EmailEnvelope(
    string From,
    string FromName,
    string To,
    string? Cc,
    string? Bcc,
    string? ReplyTo,
    string Subject,
    string Body,
    bool IsHtml
);

public interface IEmailProvider
{
    string Name { get; }
    Task SendAsync(EmailEnvelope envelope, CancellationToken cancellationToken = default);
}
```

**Template ReplyTo:** When `SendTemplatedEmailAsync` is called, the template's `DefaultReplyTo` is used unless the rendered email already has a ReplyTo (caller override wins).

**SmtpEmailProvider:** Sets `message.ReplyTo.Add(MailboxAddress.Parse(envelope.ReplyTo))` when present.

**Entity configuration:** `EmailMessageConfiguration` adds `builder.Property(e => e.ReplyTo).HasMaxLength(500)`. Same for `EmailTemplateConfiguration` with `DefaultReplyTo`.

---

## 4. Background Sending & Retry

### Problem

`SendEmailAsync` sends synchronously within the HTTP request. Failed emails are never retried despite `MaxRetryCount` setting existing.

### Design

**New flow for `SendEmailAsync`:**

1. Validate request (Section 2)
2. Create `EmailMessage` with `Status = Queued`, save to DB (gets generated ID)
3. Call `backgroundJobs.EnqueueAsync<SendEmailJob>(new SendEmailJobData(message.Id))`
4. Return the queued message immediately

**`SendEmailJob`** (implements `IModuleJob` from `SimpleModule.BackgroundJobs.Contracts`, data retrieved via `context.GetData<T>()`):

```csharp
public class SendEmailJob(
    EmailDbContext db,
    IEmailProvider emailProvider,
    IOptions<EmailModuleOptions> options,
    IEventBus eventBus,
    ILogger<SendEmailJob> logger
) : IModuleJob
{
    public async Task ExecuteAsync(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        var jobData = context.GetData<SendEmailJobData>();
        var message = await db.EmailMessages.FindAsync(jobData.MessageId);
        if (message is null) return;

        var opts = options.Value;
        var envelope = new EmailEnvelope(
            opts.DefaultFromAddress, opts.DefaultFromName,
            message.To, message.Cc, message.Bcc, message.ReplyTo,
            message.Subject, message.Body, message.IsHtml
        );

        try
        {
            context.Log($"Sending email {message.Id} to {message.To}");
            await emailProvider.SendAsync(envelope, cancellationToken);
            message.Status = EmailStatus.Sent;
            message.SentAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            eventBus.PublishInBackground(new EmailSentEvent(message.Id, message.To, message.Subject));
        }
        catch (Exception ex) when (ex is InvalidOperationException or SocketException or IOException)
        {
            message.Status = EmailStatus.Failed;
            message.ErrorMessage = ex.Message;
            await db.SaveChangesAsync(cancellationToken);

            eventBus.PublishInBackground(
                new EmailFailedEvent(message.Id, message.To, message.Subject, ex.Message));
        }
    }
}

public sealed record SendEmailJobData(EmailMessageId MessageId);
```

**`RetryFailedEmailsJob`** (also `IModuleJob`, batched to avoid loading unbounded records):

```csharp
public class RetryFailedEmailsJob(
    EmailDbContext db,
    IBackgroundJobs backgroundJobs,
    IOptions<EmailModuleOptions> options,
    IEventBus eventBus,
    ILogger<RetryFailedEmailsJob> logger
) : IModuleJob
{
    private const int BatchSize = 50;

    public async Task ExecuteAsync(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        var maxRetries = options.Value.MaxRetryCount;
        var failedMessages = await db.EmailMessages
            .Where(m => m.Status == EmailStatus.Failed && m.RetryCount < maxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        context.Log($"Found {failedMessages.Count} failed emails to retry");

        foreach (var message in failedMessages)
        {
            message.RetryCount++;
            message.Status = EmailStatus.Retrying;
            message.ErrorMessage = null;
            await db.SaveChangesAsync(cancellationToken);

            eventBus.PublishInBackground(
                new EmailRetryAttemptEvent(message.Id, message.To, message.RetryCount));

            await backgroundJobs.EnqueueAsync<SendEmailJob>(
                new SendEmailJobData(message.Id));
        }

        context.ReportProgress(100, $"Enqueued {failedMessages.Count} retries");
    }
}
```

**Registration in `EmailModule.ConfigureServices`:**

```csharp
services.AddModuleJob<SendEmailJob>();
services.AddModuleJob<RetryFailedEmailsJob>();
```

The recurring retry job is registered at startup:

```csharp
// In a startup task or IHostedService
await backgroundJobs.AddRecurringAsync<RetryFailedEmailsJob>(
    "email-retry-failed",
    "*/5 * * * *"  // every 5 minutes, configurable via settings
);
```

**New setting:** `email.retryIntervalCron` with default `"*/5 * * * *"`, registered in `ConfigureSettings`.

**New option:** `EmailModuleOptions.RetryIntervalCron` property.

---

## 5. Audit Logging

### Problem

Template CRUD operations and retry attempts have no audit trail.

### Design

**New events in `Email.Contracts/Events/`:**

```csharp
public sealed record EmailTemplateCreatedEvent(
    EmailTemplateId TemplateId, string Name, string Slug) : IEvent;

public sealed record EmailTemplateUpdatedEvent(
    EmailTemplateId TemplateId, string Name, IReadOnlyList<string> ChangedFields) : IEvent;

public sealed record EmailTemplateDeletedEvent(
    EmailTemplateId TemplateId, string Name) : IEvent;

public sealed record EmailRetryAttemptEvent(
    EmailMessageId MessageId, string To, int RetryCount) : IEvent;
```

**Publishing:** `EmailService` publishes these events after the corresponding DB operations:

- `CreateTemplateAsync` → publishes `EmailTemplateCreatedEvent`
- `UpdateTemplateAsync` → computes changed fields by comparing old vs. new values, publishes `EmailTemplateUpdatedEvent`
- `DeleteTemplateAsync` → publishes `EmailTemplateDeletedEvent`
- `RetryFailedEmailsJob` → publishes `EmailRetryAttemptEvent` per retry (already shown in Section 4)

The AuditLogs module's event bus decorator automatically captures these events — no explicit integration code needed in the Email module.

---

## 6. Template Variable Validation & HTML Escaping

### Problem

`SendTemplatedEmailAsync` silently leaves `{{variable}}` literals in rendered output when the caller doesn't provide all required variables. HTML templates are vulnerable to XSS via user-supplied variable values.

### Design

**New method on `EmailTemplateRenderer`:**

```csharp
public static HashSet<string> ExtractVariables(string template)
{
    var matches = TemplateVariablePattern().Matches(template);
    return matches.Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
}
```

**Validation in `SendTemplatedEmailAsync`:**

```csharp
var subjectVars = EmailTemplateRenderer.ExtractVariables(template.Subject);
var bodyVars = EmailTemplateRenderer.ExtractVariables(template.Body);
var allRequired = subjectVars.Union(bodyVars).ToHashSet();
var provided = variables.Keys.ToHashSet();
var missing = allRequired.Except(provided).ToList();

if (missing.Count > 0)
    throw new ValidationException($"Template '{templateSlug}' requires variables: {string.Join(", ", missing)}");
```

**HTML escaping in `EmailTemplateRenderer.Render`:**

```csharp
public static string Render(string template, Dictionary<string, string> variables, bool isHtml)
{
    return TemplateVariablePattern().Replace(template, match =>
    {
        var key = match.Groups[1].Value;
        if (!variables.TryGetValue(key, out var value))
            return match.Value;
        return isHtml ? WebUtility.HtmlEncode(value) : value;
    });
}
```

The `isHtml` parameter is passed through from `template.IsHtml` in `SendTemplatedEmailAsync`.

---

## 7. Email Statistics Dashboard

### Problem

Admins have no visibility into email health — volume, failure rates, or common errors.

### Design

**New DTO in `Email.Contracts`:**

```csharp
[Dto]
public class EmailStats
{
    public int TotalSent { get; set; }
    public int TotalFailed { get; set; }
    public int TotalQueued { get; set; }
    public int TotalRetrying { get; set; }
    public int SentLast24Hours { get; set; }
    public int FailedLast24Hours { get; set; }
    public double FailureRateLast7Days { get; set; }
    public List<ErrorSummary> TopErrors { get; set; } = [];
    public List<DailyCount> DailyVolume { get; set; } = [];
}

[Dto]
public class ErrorSummary
{
    public string ErrorMessage { get; set; } = string.Empty;
    public int Count { get; set; }
}

[Dto]
public class DailyCount
{
    public DateTime Date { get; set; }
    public int Sent { get; set; }
    public int Failed { get; set; }
}
```

**Service implementation:** `GetEmailStatsAsync` runs two `AsNoTracking` queries:

1. `GroupBy(Status)` for totals + filtered counts for last 24h and 7 days
2. `GroupBy(CreatedAt.Date)` for last 30 days of daily volume
3. Top 5 errors: `Where(Failed).GroupBy(ErrorMessage).OrderByDescending(Count).Take(5)`

**New API endpoint:** `GET /stats` with permission `Email.ViewHistory`.

**New view endpoint:** `DashboardEndpoint` as `IViewEndpoint` with `[ViewPage("Email/Dashboard")]`.

**React view `Dashboard.tsx`:**

- Summary cards at top: Sent, Failed, Queued, Retrying (each showing 24h delta)
- Failure rate badge: green (< 5%), yellow (5-15%), red (> 15%)
- Daily volume bar chart (last 30 days, sent vs. failed) using Chart from `@simplemodule/ui`
- Top errors table with error message and count

**Menu:** New "Email Dashboard" item at order 49 (above Templates at 50 and History at 51).

**Pages registry:** `'Email/Dashboard': () => import('../Views/Dashboard')` added to `Pages/index.ts`.

---

## File Inventory

**Note:** The `SendEmailJob` reuses the existing `EmailSentEvent` and `EmailFailedEvent` from `Email.Contracts/Events/` — no new event files needed for those.

### New Files (~17)

| File | Purpose |
|---|---|
| `Contracts/QueryEmailMessagesRequest.cs` | Message query DTO |
| `Contracts/QueryEmailTemplatesRequest.cs` | Template query DTO |
| `Contracts/EmailStats.cs` | Stats DTO with ErrorSummary, DailyCount |
| `Contracts/Events/EmailTemplateCreatedEvent.cs` | Audit event |
| `Contracts/Events/EmailTemplateUpdatedEvent.cs` | Audit event |
| `Contracts/Events/EmailTemplateDeletedEvent.cs` | Audit event |
| `Contracts/Events/EmailRetryAttemptEvent.cs` | Audit event |
| `Providers/EmailEnvelope.cs` | Parameter object for IEmailProvider |
| `Validators/SendEmailRequestValidator.cs` | Request validation |
| `Validators/CreateEmailTemplateRequestValidator.cs` | Request validation |
| `Validators/UpdateEmailTemplateRequestValidator.cs` | Request validation |
| `Jobs/SendEmailJob.cs` | Background email send |
| `Jobs/SendEmailJobData.cs` | Job data record |
| `Jobs/RetryFailedEmailsJob.cs` | Recurring retry job |
| `Views/Dashboard.tsx` | Stats dashboard React view |
| `Views/DashboardEndpoint.cs` | Dashboard view endpoint |
| `Endpoints/Stats/GetStatsEndpoint.cs` | Stats API endpoint |

### Modified Files (~20)

| File | Changes |
|---|---|
| `Contracts/IEmailContracts.cs` | Add QueryMessagesAsync, QueryTemplatesAsync, GetEmailStatsAsync; remove GetAllMessagesAsync, GetAllTemplatesAsync |
| `Contracts/SendEmailRequest.cs` | Add ReplyTo field |
| `Contracts/EmailMessage.cs` | Add ReplyTo field |
| `Contracts/CreateEmailTemplateRequest.cs` | Add DefaultReplyTo field |
| `Contracts/UpdateEmailTemplateRequest.cs` | Add DefaultReplyTo field |
| `Contracts/EmailTemplate.cs` | Add DefaultReplyTo field |
| `EmailService.cs` | Async send via jobs, pagination queries, stats query, audit events, validation calls |
| `EmailModule.cs` | Register jobs, add BackgroundJobs dependency, add new settings |
| `EmailModuleOptions.cs` | Add RetryIntervalCron property |
| `Providers/IEmailProvider.cs` | Change SendAsync to accept EmailEnvelope |
| `Providers/SmtpEmailProvider.cs` | Use EmailEnvelope, add ReplyTo support |
| `Providers/LogEmailProvider.cs` | Use EmailEnvelope |
| `Services/EmailTemplateRenderer.cs` | Add ExtractVariables, add isHtml param for HTML escaping |
| `EntityConfigurations/EmailMessageConfiguration.cs` | Add ReplyTo column |
| `EntityConfigurations/EmailTemplateConfiguration.cs` | Add DefaultReplyTo column |
| `Views/History.tsx` | Pagination, filters, use paginated props |
| `Views/Templates.tsx` | Pagination, search, use paginated props |
| `Pages/index.ts` | Add Dashboard entry |
| `Locales/en.json` | Add dashboard and filter translation keys |
| `Locales/keys.ts` | Add dashboard and filter keys |
| `SimpleModule.Email.csproj` | Add BackgroundJobs.Contracts reference |
| `types.ts` | Will be regenerated by source generator |

---

## Testing Strategy

**Unit tests (EmailServiceTests):**

- Pagination: verify page/pageSize/filters produce correct results
- Validation: each validator with valid, invalid, and edge-case inputs
- Template variable extraction and missing variable detection
- HTML escaping in template rendering
- Stats aggregation with various message states

**Job tests:**

- `SendEmailJob`: success path (status → Sent), failure path (status → Failed, error stored)
- `RetryFailedEmailsJob`: picks up failed messages under max retry, skips those at max, sets Retrying status

**Integration tests (if added):**

- Full flow: send request → job executes → message status updated
- Pagination endpoint returns correct page counts
- Validation returns 400 with problem details

---

## Migration Notes

- New columns (`ReplyTo` on EmailMessage, `DefaultReplyTo` on EmailTemplate) are nullable — no data migration needed
- `SendEmailAsync` behavior changes from synchronous to queued — the returned message will have `Status = Queued` instead of `Sent`. Current callers: (1) `IdentityEmailSender` — does not check return status, just awaits completion, safe. (2) API endpoint `SendEmailEndpoint` — returns the message to the client, which will now see `Queued` status; acceptable since the History UI shows status badges. (3) `SendTemplatedEmailAsync` — delegates to `SendEmailAsync`, same change applies. No caller currently depends on receiving `Sent` status synchronously.
- `IdentityEmailSender` calls `SendEmailAsync` which now queues — Identity confirmation emails will be sent asynchronously (acceptable; ASP.NET Identity does not check the email was actually delivered, only that the method completed)
- The `GetAllMessagesAsync`/`GetAllTemplatesAsync` removal from `IEmailContracts` is a breaking contract change — no external modules currently call these (only the Email module's own views and endpoints use them), but any future cross-module callers should use the paginated versions
