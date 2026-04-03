# Email Module Production Hardening — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Harden the Email module for medium-volume production use with pagination, validation, background sending with retry, audit logging, template safety, and a statistics dashboard.

**Architecture:** Layered additions on top of the existing `EmailService` orchestrator. Background sending delegates to `IBackgroundJobs` from the BackgroundJobs module. All new DTOs go in `Email.Contracts`. Validators are static classes colocated with endpoints. React views use server-side pagination matching the AuditLogs Browse pattern.

**Tech Stack:** .NET 10, EF Core, MailKit, BackgroundJobs (TickerQ), React 19, Inertia.js, recharts, @simplemodule/ui

**Spec:** `docs/superpowers/specs/2026-04-03-email-module-hardening-design.md`

---

## Chunk 1: Contracts, Entities, and Provider Refactor

These tasks lay the foundation — new DTOs, entity changes, and the EmailEnvelope refactor. No behavior changes yet.

### Task 1: Add ReplyTo fields to entities and contracts

**Files:**
- Modify: `modules/Email/src/SimpleModule.Email.Contracts/SendEmailRequest.cs`
- Modify: `modules/Email/src/SimpleModule.Email.Contracts/EmailMessage.cs`
- Modify: `modules/Email/src/SimpleModule.Email.Contracts/EmailTemplate.cs`
- Modify: `modules/Email/src/SimpleModule.Email.Contracts/CreateEmailTemplateRequest.cs`
- Modify: `modules/Email/src/SimpleModule.Email.Contracts/UpdateEmailTemplateRequest.cs`
- Modify: `modules/Email/src/SimpleModule.Email/EntityConfigurations/EmailMessageConfiguration.cs`
- Modify: `modules/Email/src/SimpleModule.Email/EntityConfigurations/EmailTemplateConfiguration.cs`

- [ ] **Step 1: Add ReplyTo to SendEmailRequest**

Add `public string? ReplyTo { get; set; }` to `SendEmailRequest.cs`.

- [ ] **Step 2: Add ReplyTo to EmailMessage entity**

Add `public string? ReplyTo { get; set; }` to `EmailMessage.cs`.

- [ ] **Step 3: Add DefaultReplyTo to EmailTemplate entity**

Add `public string? DefaultReplyTo { get; set; }` to `EmailTemplate.cs`.

- [ ] **Step 4: Add DefaultReplyTo to CreateEmailTemplateRequest**

Add `public string? DefaultReplyTo { get; set; }` to `CreateEmailTemplateRequest.cs`.

- [ ] **Step 5: Add DefaultReplyTo to UpdateEmailTemplateRequest**

Add `public string? DefaultReplyTo { get; set; }` to `UpdateEmailTemplateRequest.cs`.

- [ ] **Step 6: Configure ReplyTo column in EmailMessageConfiguration**

Add after the existing `Provider` property:

```csharp
builder.Property(e => e.ReplyTo).HasMaxLength(500);
```

- [ ] **Step 7: Configure DefaultReplyTo column in EmailTemplateConfiguration**

Add after the existing `Body` property:

```csharp
builder.Property(e => e.DefaultReplyTo).HasMaxLength(500);
```

- [ ] **Step 8: Build to verify**

Run: `dotnet build modules/Email/src/SimpleModule.Email/SimpleModule.Email.csproj`
Expected: Build succeeded, 0 warnings, 0 errors.

- [ ] **Step 9: Commit**

```bash
git add modules/Email/src/SimpleModule.Email.Contracts/ modules/Email/src/SimpleModule.Email/EntityConfigurations/
git commit -m "feat(email): add ReplyTo and DefaultReplyTo fields to entities and contracts"
```

---

### Task 2: Add pagination and stats DTOs to contracts

**Files:**
- Create: `modules/Email/src/SimpleModule.Email.Contracts/QueryEmailMessagesRequest.cs`
- Create: `modules/Email/src/SimpleModule.Email.Contracts/QueryEmailTemplatesRequest.cs`
- Create: `modules/Email/src/SimpleModule.Email.Contracts/EmailStats.cs`
- Modify: `modules/Email/src/SimpleModule.Email.Contracts/IEmailContracts.cs`

- [ ] **Step 1: Create QueryEmailMessagesRequest**

```csharp
using SimpleModule.Core;

namespace SimpleModule.Email.Contracts;

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
```

- [ ] **Step 2: Create QueryEmailTemplatesRequest**

```csharp
using SimpleModule.Core;

namespace SimpleModule.Email.Contracts;

[Dto]
public class QueryEmailTemplatesRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
}
```

- [ ] **Step 3: Create EmailStats with ErrorSummary and DailyCount**

```csharp
using SimpleModule.Core;

namespace SimpleModule.Email.Contracts;

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

- [ ] **Step 4: Update IEmailContracts**

Replace `GetAllMessagesAsync` and `GetAllTemplatesAsync` with paginated versions, add stats:

```csharp
namespace SimpleModule.Email.Contracts;

using SimpleModule.Core;

public interface IEmailContracts
{
    Task<EmailMessage> SendEmailAsync(SendEmailRequest request);
    Task<EmailMessage> SendTemplatedEmailAsync(
        string templateSlug,
        string to,
        Dictionary<string, string> variables
    );
    Task<PagedResult<EmailMessage>> QueryMessagesAsync(QueryEmailMessagesRequest request);
    Task<EmailMessage?> GetMessageByIdAsync(EmailMessageId id);
    Task<PagedResult<EmailTemplate>> QueryTemplatesAsync(QueryEmailTemplatesRequest request);
    Task<EmailTemplate?> GetTemplateByIdAsync(EmailTemplateId id);
    Task<EmailTemplate?> GetTemplateBySlugAsync(string slug);
    Task<EmailTemplate> CreateTemplateAsync(CreateEmailTemplateRequest request);
    Task<EmailTemplate> UpdateTemplateAsync(EmailTemplateId id, UpdateEmailTemplateRequest request);
    Task DeleteTemplateAsync(EmailTemplateId id);
    Task<EmailStats> GetEmailStatsAsync();
}
```

- [ ] **Step 5: Build to verify contracts compile**

Run: `dotnet build modules/Email/src/SimpleModule.Email.Contracts/SimpleModule.Email.Contracts.csproj`
Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
git add modules/Email/src/SimpleModule.Email.Contracts/
git commit -m "feat(email): add pagination DTOs, stats DTO, and update IEmailContracts"
```

---

### Task 3: Refactor IEmailProvider to use EmailEnvelope

**Files:**
- Create: `modules/Email/src/SimpleModule.Email/Providers/EmailEnvelope.cs`
- Modify: `modules/Email/src/SimpleModule.Email/Providers/IEmailProvider.cs`
- Modify: `modules/Email/src/SimpleModule.Email/Providers/SmtpEmailProvider.cs`
- Modify: `modules/Email/src/SimpleModule.Email/Providers/LogEmailProvider.cs`
- Test: `modules/Email/tests/SimpleModule.Email.Tests/Unit/EmailServiceTests.cs`

- [ ] **Step 1: Create EmailEnvelope record**

```csharp
namespace SimpleModule.Email.Providers;

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
```

- [ ] **Step 2: Update IEmailProvider interface**

```csharp
namespace SimpleModule.Email.Providers;

public interface IEmailProvider
{
    string Name { get; }
    Task SendAsync(EmailEnvelope envelope, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 3: Update SmtpEmailProvider**

Replace the 8-param `SendAsync` with the envelope version. Key changes:
- Accept `EmailEnvelope envelope` parameter
- In `CreateMessage`, use `envelope.From`, `envelope.FromName`, etc.
- Add ReplyTo support: `if (!string.IsNullOrWhiteSpace(envelope.ReplyTo)) message.ReplyTo.Add(MailboxAddress.Parse(envelope.ReplyTo));`
- The `CreateMessage` method becomes a private helper that takes the envelope.

```csharp
public async Task SendAsync(EmailEnvelope envelope, CancellationToken cancellationToken = default)
{
    var smtp = options.Value.Smtp;
    using var message = CreateMessage(envelope);

    using var client = new SmtpClient();
    await client.ConnectAsync(smtp.Host, smtp.Port, smtp.UseSsl, cancellationToken);

    if (!string.IsNullOrWhiteSpace(smtp.Username))
    {
        await client.AuthenticateAsync(smtp.Username, smtp.Password, cancellationToken);
    }

    await client.SendAsync(message, cancellationToken);
    await client.DisconnectAsync(true, cancellationToken);

    LogEmailSent(logger, envelope.To, envelope.Subject);
}

private static MimeMessage CreateMessage(EmailEnvelope envelope)
{
    var message = new MimeMessage();
    message.From.Add(new MailboxAddress(envelope.FromName, envelope.From));
    message.To.Add(MailboxAddress.Parse(envelope.To));

    if (!string.IsNullOrWhiteSpace(envelope.Cc))
        message.Cc.Add(MailboxAddress.Parse(envelope.Cc));

    if (!string.IsNullOrWhiteSpace(envelope.Bcc))
        message.Bcc.Add(MailboxAddress.Parse(envelope.Bcc));

    if (!string.IsNullOrWhiteSpace(envelope.ReplyTo))
        message.ReplyTo.Add(MailboxAddress.Parse(envelope.ReplyTo));

    message.Subject = envelope.Subject;
    message.Body = new TextPart(envelope.IsHtml ? "html" : "plain") { Text = envelope.Body };

    return message;
}
```

- [ ] **Step 4: Update LogEmailProvider**

```csharp
public Task SendAsync(EmailEnvelope envelope, CancellationToken cancellationToken = default)
{
    LogEmail(logger, envelope.From, envelope.To, envelope.Subject, envelope.Body);
    return Task.CompletedTask;
}
```

- [ ] **Step 5: Update EmailService.SendEmailAsync to build envelope**

In `EmailService.SendEmailAsync`, replace the 8-param `emailProvider.SendAsync(...)` call with:

```csharp
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
```

Also store ReplyTo on the message entity: `ReplyTo = request.ReplyTo,` in the initializer.

- [ ] **Step 6: Run existing tests to verify refactor didn't break anything**

Run: `dotnet test modules/Email/tests/SimpleModule.Email.Tests/`
Expected: All 13 tests pass.

- [ ] **Step 7: Commit**

```bash
git add modules/Email/src/SimpleModule.Email/Providers/ modules/Email/src/SimpleModule.Email/EmailService.cs
git commit -m "refactor(email): replace IEmailProvider parameter sprawl with EmailEnvelope"
```

---

### Task 4: Add new audit events to contracts

**Files:**
- Create: `modules/Email/src/SimpleModule.Email.Contracts/Events/EmailTemplateCreatedEvent.cs`
- Create: `modules/Email/src/SimpleModule.Email.Contracts/Events/EmailTemplateUpdatedEvent.cs`
- Create: `modules/Email/src/SimpleModule.Email.Contracts/Events/EmailTemplateDeletedEvent.cs`
- Create: `modules/Email/src/SimpleModule.Email.Contracts/Events/EmailRetryAttemptEvent.cs`

- [ ] **Step 1: Create all four event records**

```csharp
// EmailTemplateCreatedEvent.cs
using SimpleModule.Core.Events;

namespace SimpleModule.Email.Contracts.Events;

public sealed record EmailTemplateCreatedEvent(
    EmailTemplateId TemplateId, string Name, string Slug) : IEvent;
```

```csharp
// EmailTemplateUpdatedEvent.cs
using SimpleModule.Core.Events;

namespace SimpleModule.Email.Contracts.Events;

public sealed record EmailTemplateUpdatedEvent(
    EmailTemplateId TemplateId, string Name, IReadOnlyList<string> ChangedFields) : IEvent;
```

```csharp
// EmailTemplateDeletedEvent.cs
using SimpleModule.Core.Events;

namespace SimpleModule.Email.Contracts.Events;

public sealed record EmailTemplateDeletedEvent(
    EmailTemplateId TemplateId, string Name) : IEvent;
```

```csharp
// EmailRetryAttemptEvent.cs
using SimpleModule.Core.Events;

namespace SimpleModule.Email.Contracts.Events;

public sealed record EmailRetryAttemptEvent(
    EmailMessageId MessageId, string To, int RetryCount) : IEvent;
```

- [ ] **Step 2: Build contracts to verify**

Run: `dotnet build modules/Email/src/SimpleModule.Email.Contracts/SimpleModule.Email.Contracts.csproj`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add modules/Email/src/SimpleModule.Email.Contracts/Events/
git commit -m "feat(email): add audit events for template CRUD and retry attempts"
```

---

## Chunk 2: Validators and Template Safety

### Task 5: Create request validators

**Files:**
- Create: `modules/Email/src/SimpleModule.Email/Validators/SendEmailRequestValidator.cs`
- Create: `modules/Email/src/SimpleModule.Email/Validators/CreateEmailTemplateRequestValidator.cs`
- Create: `modules/Email/src/SimpleModule.Email/Validators/UpdateEmailTemplateRequestValidator.cs`
- Test: `modules/Email/tests/SimpleModule.Email.Tests/Unit/ValidatorTests.cs`

- [ ] **Step 1: Write failing tests for SendEmailRequestValidator**

Create `modules/Email/tests/SimpleModule.Email.Tests/Unit/ValidatorTests.cs`:

```csharp
using FluentAssertions;
using SimpleModule.Email.Contracts;
using SimpleModule.Email.Validators;

namespace SimpleModule.Email.Tests.Unit;

public sealed class ValidatorTests
{
    [Fact]
    public void SendEmailRequestValidator_WithValidRequest_ReturnsSuccess()
    {
        var request = new SendEmailRequest
        {
            To = "test@example.com",
            Subject = "Test",
            Body = "Hello",
        };

        var result = SendEmailRequestValidator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SendEmailRequestValidator_WithEmptyTo_ReturnsError()
    {
        var request = new SendEmailRequest
        {
            To = "",
            Subject = "Test",
            Body = "Hello",
        };

        var result = SendEmailRequestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("To");
    }

    [Fact]
    public void SendEmailRequestValidator_WithInvalidEmail_ReturnsError()
    {
        var request = new SendEmailRequest
        {
            To = "not-an-email",
            Subject = "Test",
            Body = "Hello",
        };

        var result = SendEmailRequestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("To");
    }

    [Fact]
    public void SendEmailRequestValidator_WithInvalidReplyTo_ReturnsError()
    {
        var request = new SendEmailRequest
        {
            To = "test@example.com",
            ReplyTo = "bad-email",
            Subject = "Test",
            Body = "Hello",
        };

        var result = SendEmailRequestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("ReplyTo");
    }

    [Fact]
    public void CreateEmailTemplateRequestValidator_WithValidRequest_ReturnsSuccess()
    {
        var request = new CreateEmailTemplateRequest
        {
            Name = "Welcome",
            Slug = "welcome-email",
            Subject = "Welcome {{name}}",
            Body = "Hello!",
        };

        var result = CreateEmailTemplateRequestValidator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateEmailTemplateRequestValidator_WithInvalidSlug_ReturnsError()
    {
        var request = new CreateEmailTemplateRequest
        {
            Name = "Welcome",
            Slug = "INVALID SLUG!",
            Subject = "Welcome",
            Body = "Hello!",
        };

        var result = CreateEmailTemplateRequestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("Slug");
    }

    [Fact]
    public void UpdateEmailTemplateRequestValidator_WithEmptyName_ReturnsError()
    {
        var request = new UpdateEmailTemplateRequest
        {
            Name = "",
            Subject = "Test",
            Body = "Hello!",
        };

        var result = UpdateEmailTemplateRequestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("Name");
    }
}
```

- [ ] **Step 2: Run tests — verify they fail (validators don't exist yet)**

Run: `dotnet test modules/Email/tests/SimpleModule.Email.Tests/ --filter "ValidatorTests"`
Expected: Build failure — `SendEmailRequestValidator` not found.

- [ ] **Step 3: Implement SendEmailRequestValidator**

```csharp
using System.Text.RegularExpressions;
using SimpleModule.Core.Validation;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Validators;

public static partial class SendEmailRequestValidator
{
    public static ValidationResult Validate(SendEmailRequest request) =>
        new ValidationBuilder()
            .AddErrorIf(string.IsNullOrWhiteSpace(request.To), "To", "Recipient is required.")
            .AddErrorIf(
                !string.IsNullOrWhiteSpace(request.To) && !EmailPattern().IsMatch(request.To),
                "To",
                "Invalid email format."
            )
            .AddErrorIf(
                !string.IsNullOrWhiteSpace(request.ReplyTo)
                    && !EmailPattern().IsMatch(request.ReplyTo),
                "ReplyTo",
                "Invalid email format."
            )
            .AddErrorIf(
                string.IsNullOrWhiteSpace(request.Subject),
                "Subject",
                "Subject is required."
            )
            .AddErrorIf(
                request.Subject?.Length > 500,
                "Subject",
                "Subject must not exceed 500 characters."
            )
            .AddErrorIf(string.IsNullOrWhiteSpace(request.Body), "Body", "Body is required.")
            .Build();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    internal static partial Regex EmailPattern();
}
```

- [ ] **Step 4: Implement CreateEmailTemplateRequestValidator**

```csharp
using System.Text.RegularExpressions;
using SimpleModule.Core.Validation;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Validators;

public static partial class CreateEmailTemplateRequestValidator
{
    public static ValidationResult Validate(CreateEmailTemplateRequest request) =>
        new ValidationBuilder()
            .AddErrorIf(string.IsNullOrWhiteSpace(request.Name), "Name", "Name is required.")
            .AddErrorIf(
                request.Name?.Length > 200,
                "Name",
                "Name must not exceed 200 characters."
            )
            .AddErrorIf(string.IsNullOrWhiteSpace(request.Slug), "Slug", "Slug is required.")
            .AddErrorIf(
                request.Slug?.Length > 200,
                "Slug",
                "Slug must not exceed 200 characters."
            )
            .AddErrorIf(
                !string.IsNullOrWhiteSpace(request.Slug) && !SlugPattern().IsMatch(request.Slug),
                "Slug",
                "Slug must be lowercase alphanumeric with hyphens."
            )
            .AddErrorIf(
                string.IsNullOrWhiteSpace(request.Subject),
                "Subject",
                "Subject is required."
            )
            .AddErrorIf(
                request.Subject?.Length > 500,
                "Subject",
                "Subject must not exceed 500 characters."
            )
            .AddErrorIf(string.IsNullOrWhiteSpace(request.Body), "Body", "Body is required.")
            .AddErrorIf(
                !string.IsNullOrWhiteSpace(request.DefaultReplyTo)
                    && !SendEmailRequestValidator.EmailPattern().IsMatch(request.DefaultReplyTo),
                "DefaultReplyTo",
                "Invalid email format."
            )
            .Build();

    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
```

Note: `SendEmailRequestValidator.EmailPattern()` needs to be made `internal` instead of `private` so it can be reused. Update its accessibility.

- [ ] **Step 5: Implement UpdateEmailTemplateRequestValidator**

```csharp
using SimpleModule.Core.Validation;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Validators;

public static class UpdateEmailTemplateRequestValidator
{
    public static ValidationResult Validate(UpdateEmailTemplateRequest request) =>
        new ValidationBuilder()
            .AddErrorIf(string.IsNullOrWhiteSpace(request.Name), "Name", "Name is required.")
            .AddErrorIf(
                request.Name?.Length > 200,
                "Name",
                "Name must not exceed 200 characters."
            )
            .AddErrorIf(
                string.IsNullOrWhiteSpace(request.Subject),
                "Subject",
                "Subject is required."
            )
            .AddErrorIf(
                request.Subject?.Length > 500,
                "Subject",
                "Subject must not exceed 500 characters."
            )
            .AddErrorIf(string.IsNullOrWhiteSpace(request.Body), "Body", "Body is required.")
            .AddErrorIf(
                !string.IsNullOrWhiteSpace(request.DefaultReplyTo)
                    && !SendEmailRequestValidator.EmailPattern().IsMatch(request.DefaultReplyTo),
                "DefaultReplyTo",
                "Invalid email format."
            )
            .Build();
}
```

- [ ] **Step 6: Run tests — verify they pass**

Run: `dotnet test modules/Email/tests/SimpleModule.Email.Tests/ --filter "ValidatorTests"`
Expected: All 7 tests pass.

- [ ] **Step 7: Commit**

```bash
git add modules/Email/src/SimpleModule.Email/Validators/ modules/Email/tests/SimpleModule.Email.Tests/Unit/ValidatorTests.cs
git commit -m "feat(email): add request validators for send, create template, update template"
```

---

### Task 6: Add template variable validation and HTML escaping

**Files:**
- Modify: `modules/Email/src/SimpleModule.Email/Services/EmailTemplateRenderer.cs`
- Test: `modules/Email/tests/SimpleModule.Email.Tests/Unit/EmailTemplateRendererTests.cs`

- [ ] **Step 1: Write failing tests**

Create `modules/Email/tests/SimpleModule.Email.Tests/Unit/EmailTemplateRendererTests.cs`:

```csharp
using FluentAssertions;
using SimpleModule.Email.Services;

namespace SimpleModule.Email.Tests.Unit;

public sealed class EmailTemplateRendererTests
{
    [Fact]
    public void ExtractVariables_FindsAllVariables()
    {
        var template = "Hello {{name}}, welcome to {{app}}!";

        var vars = EmailTemplateRenderer.ExtractVariables(template);

        vars.Should().BeEquivalentTo(["name", "app"]);
    }

    [Fact]
    public void ExtractVariables_WithNoVariables_ReturnsEmpty()
    {
        var vars = EmailTemplateRenderer.ExtractVariables("No variables here.");

        vars.Should().BeEmpty();
    }

    [Fact]
    public void Render_WithHtmlTrue_EscapesValues()
    {
        var template = "Hello {{name}}";
        var vars = new Dictionary<string, string> { ["name"] = "<script>alert('xss')</script>" };

        var result = EmailTemplateRenderer.Render(template, vars, isHtml: true);

        result.Should().Be("Hello &lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;");
    }

    [Fact]
    public void Render_WithHtmlFalse_DoesNotEscape()
    {
        var template = "Hello {{name}}";
        var vars = new Dictionary<string, string> { ["name"] = "<b>John</b>" };

        var result = EmailTemplateRenderer.Render(template, vars, isHtml: false);

        result.Should().Be("Hello <b>John</b>");
    }

    [Fact]
    public void Render_WithMissingVariable_LeavesPlaceholder()
    {
        var template = "Hello {{name}}, your code is {{code}}";
        var vars = new Dictionary<string, string> { ["name"] = "John" };

        var result = EmailTemplateRenderer.Render(template, vars, isHtml: false);

        result.Should().Be("Hello John, your code is {{code}}");
    }
}
```

- [ ] **Step 2: Run tests — verify they fail**

Run: `dotnet test modules/Email/tests/SimpleModule.Email.Tests/ --filter "EmailTemplateRendererTests"`
Expected: Build failure or test failure (signature mismatch, ExtractVariables doesn't exist).

- [ ] **Step 3: Update EmailTemplateRenderer**

```csharp
using System.Net;
using System.Text.RegularExpressions;

namespace SimpleModule.Email.Services;

public static partial class EmailTemplateRenderer
{
    public static string Render(string template, Dictionary<string, string> variables, bool isHtml)
    {
        return TemplateVariablePattern()
            .Replace(
                template,
                match =>
                {
                    var key = match.Groups[1].Value;
                    if (!variables.TryGetValue(key, out var value))
                        return match.Value;
                    return isHtml ? WebUtility.HtmlEncode(value) : value;
                }
            );
    }

    public static HashSet<string> ExtractVariables(string template)
    {
        var matches = TemplateVariablePattern().Matches(template);
        return matches.Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
    }

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex TemplateVariablePattern();
}
```

- [ ] **Step 4: Update EmailService.SendTemplatedEmailAsync to use new Render signature and validate variables**

In `EmailService.SendTemplatedEmailAsync`, after loading the template:

```csharp
var subjectVars = EmailTemplateRenderer.ExtractVariables(template.Subject);
var bodyVars = EmailTemplateRenderer.ExtractVariables(template.Body);
var allRequired = subjectVars.Union(bodyVars).ToHashSet();
var missing = allRequired.Except(variables.Keys).ToList();

if (missing.Count > 0)
{
    throw new Core.Exceptions.ValidationException(
        new Dictionary<string, string[]>
        {
            ["variables"] = [$"Template '{templateSlug}' requires variables: {string.Join(", ", missing)}"],
        }
    );
}

var renderedSubject = EmailTemplateRenderer.Render(template.Subject, variables, template.IsHtml);
var renderedBody = EmailTemplateRenderer.Render(template.Body, variables, template.IsHtml);
```

Also set ReplyTo from template default when sending:

```csharp
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
```

- [ ] **Step 5: Fix the existing Render call site in EmailService (it has the old 2-param signature)**

The old call `EmailTemplateRenderer.Render(template.Subject, variables)` now needs the third `isHtml` parameter. Update both calls to pass `template.IsHtml`.

- [ ] **Step 6: Run all tests**

Run: `dotnet test modules/Email/tests/SimpleModule.Email.Tests/`
Expected: All tests pass (existing + new renderer tests).

- [ ] **Step 7: Commit**

```bash
git add modules/Email/src/SimpleModule.Email/Services/EmailTemplateRenderer.cs modules/Email/src/SimpleModule.Email/EmailService.cs modules/Email/tests/SimpleModule.Email.Tests/Unit/EmailTemplateRendererTests.cs
git commit -m "feat(email): add template variable validation and HTML escaping"
```

---

## Chunk 3: EmailService — Pagination, Stats, Audit Events, Validation Integration

### Task 7: Implement pagination queries and stats in EmailService

**Files:**
- Modify: `modules/Email/src/SimpleModule.Email/EmailService.cs`
- Test: `modules/Email/tests/SimpleModule.Email.Tests/Unit/EmailServiceTests.cs`

- [ ] **Step 1: Write failing tests for QueryMessagesAsync**

Add to `EmailServiceTests.cs`:

```csharp
[Fact]
public async Task QueryMessagesAsync_WithDefaults_ReturnsPaginatedResult()
{
    // Seed 3 messages
    for (var i = 0; i < 3; i++)
    {
        await _sut.SendEmailAsync(new SendEmailRequest
        {
            To = $"user{i}@test.com",
            Subject = $"Subject {i}",
            Body = "Body",
        });
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
        await _sut.SendEmailAsync(new SendEmailRequest
        {
            To = $"user{i}@test.com",
            Subject = $"Subject {i}",
            Body = "Body",
        });
    }

    var result = await _sut.QueryMessagesAsync(
        new QueryEmailMessagesRequest { PageSize = 1 }
    );

    result.Items.Should().HaveCount(1);
    result.TotalCount.Should().Be(3);
}

[Fact]
public async Task QueryMessagesAsync_FilterByTo_ReturnsMatching()
{
    await _sut.SendEmailAsync(new SendEmailRequest { To = "alice@test.com", Subject = "A", Body = "B" });
    await _sut.SendEmailAsync(new SendEmailRequest { To = "bob@test.com", Subject = "A", Body = "B" });

    var result = await _sut.QueryMessagesAsync(
        new QueryEmailMessagesRequest { To = "alice" }
    );

    result.Items.Should().HaveCount(1);
    result.Items[0].To.Should().Be("alice@test.com");
}
```

- [ ] **Step 2: Write failing tests for QueryTemplatesAsync**

```csharp
[Fact]
public async Task QueryTemplatesAsync_WithSearch_FiltersResults()
{
    await _sut.CreateTemplateAsync(new CreateEmailTemplateRequest { Name = "Welcome", Slug = "welcome", Subject = "S", Body = "B" });
    await _sut.CreateTemplateAsync(new CreateEmailTemplateRequest { Name = "Goodbye", Slug = "goodbye", Subject = "S", Body = "B" });

    var result = await _sut.QueryTemplatesAsync(
        new QueryEmailTemplatesRequest { Search = "welcome" }
    );

    result.Items.Should().HaveCount(1);
    result.Items[0].Name.Should().Be("Welcome");
}
```

- [ ] **Step 3: Write failing test for GetEmailStatsAsync**

Note: This test seeds messages directly in the DB with specific statuses rather than calling `SendEmailAsync`, because after Task 9 `SendEmailAsync` will create messages with `Queued` status (not `Sent`). Direct seeding makes this test independent of the send flow.

```csharp
[Fact]
public async Task GetEmailStatsAsync_ReturnsCorrectCounts()
{
    // Seed messages directly with specific statuses
    _db.EmailMessages.AddRange(
        new EmailMessage { To = "a@test.com", Subject = "A", Body = "B", Status = EmailStatus.Sent, CreatedAt = DateTime.UtcNow, SentAt = DateTime.UtcNow },
        new EmailMessage { To = "b@test.com", Subject = "B", Body = "B", Status = EmailStatus.Sent, CreatedAt = DateTime.UtcNow, SentAt = DateTime.UtcNow },
        new EmailMessage { To = "c@test.com", Subject = "C", Body = "B", Status = EmailStatus.Failed, ErrorMessage = "Timeout", CreatedAt = DateTime.UtcNow }
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
```

- [ ] **Step 4: Implement QueryMessagesAsync in EmailService**

Follow the AuditLogs `QueryAsync` pattern:

```csharp
public async Task<PagedResult<EmailMessage>> QueryMessagesAsync(QueryEmailMessagesRequest request)
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

    query = request.SortBy switch
    {
        "To" => request.SortDescending
            ? query.OrderByDescending(m => m.To)
            : query.OrderBy(m => m.To),
        "Subject" => request.SortDescending
            ? query.OrderByDescending(m => m.Subject)
            : query.OrderBy(m => m.Subject),
        "Status" => request.SortDescending
            ? query.OrderByDescending(m => m.Status)
            : query.OrderBy(m => m.Status),
        // Default sorts by Id (auto-increment, correlates with CreatedAt) to avoid
        // SQLite DateTimeOffset issues, matching the AuditLogs pattern.
        _ => request.SortDescending
            ? query.OrderByDescending(m => m.Id)
            : query.OrderBy(m => m.Id),
    };

    var page = Math.Max(1, request.Page);
    var pageSize = Math.Clamp(request.PageSize, 1, 100);

    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return new PagedResult<EmailMessage>
    {
        Items = items,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
    };
}
```

- [ ] **Step 5: Implement QueryTemplatesAsync**

```csharp
public async Task<PagedResult<EmailTemplate>> QueryTemplatesAsync(QueryEmailTemplatesRequest request)
{
    var query = db.EmailTemplates.AsNoTracking().AsQueryable();

    if (!string.IsNullOrWhiteSpace(request.Search))
    {
        query = query.Where(t =>
            t.Name.Contains(request.Search) || t.Slug.Contains(request.Search)
        );
    }

    var totalCount = await query.CountAsync();
    var page = Math.Max(1, request.Page);
    var pageSize = Math.Clamp(request.PageSize, 1, 100);

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
```

- [ ] **Step 6: Implement GetEmailStatsAsync**

```csharp
public async Task<EmailStats> GetEmailStatsAsync()
{
    var now = DateTime.UtcNow;
    var last24Hours = now.AddHours(-24);
    var last7Days = now.AddDays(-7);
    var last30Days = now.AddDays(-30);

    var allMessages = db.EmailMessages.AsNoTracking();

    var statusCounts = await allMessages
        .GroupBy(m => m.Status)
        .Select(g => new { Status = g.Key, Count = g.Count() })
        .ToListAsync();

    var sent24h = await allMessages.CountAsync(m => m.Status == EmailStatus.Sent && m.CreatedAt >= last24Hours);
    var failed24h = await allMessages.CountAsync(m => m.Status == EmailStatus.Failed && m.CreatedAt >= last24Hours);

    var sent7d = await allMessages.CountAsync(m => m.Status == EmailStatus.Sent && m.CreatedAt >= last7Days);
    var failed7d = await allMessages.CountAsync(m => m.Status == EmailStatus.Failed && m.CreatedAt >= last7Days);
    var total7d = await allMessages.CountAsync(m => m.CreatedAt >= last7Days);
    var failureRate = total7d > 0 ? (double)failed7d / total7d * 100 : 0;

    var topErrors = await allMessages
        .Where(m => m.Status == EmailStatus.Failed && m.ErrorMessage != null)
        .GroupBy(m => m.ErrorMessage!)
        .OrderByDescending(g => g.Count())
        .Take(5)
        .Select(g => new ErrorSummary { ErrorMessage = g.Key, Count = g.Count() })
        .ToListAsync();

    var dailyVolume = await allMessages
        .Where(m => m.CreatedAt >= last30Days)
        .GroupBy(m => m.CreatedAt.Date)
        .Select(g => new DailyCount
        {
            Date = g.Key,
            Sent = g.Count(m => m.Status == EmailStatus.Sent),
            Failed = g.Count(m => m.Status == EmailStatus.Failed),
        })
        .OrderBy(d => d.Date)
        .ToListAsync();

    return new EmailStats
    {
        TotalSent = statusCounts.FirstOrDefault(s => s.Status == EmailStatus.Sent)?.Count ?? 0,
        TotalFailed = statusCounts.FirstOrDefault(s => s.Status == EmailStatus.Failed)?.Count ?? 0,
        TotalQueued = statusCounts.FirstOrDefault(s => s.Status == EmailStatus.Queued)?.Count ?? 0,
        TotalRetrying = statusCounts.FirstOrDefault(s => s.Status == EmailStatus.Retrying)?.Count ?? 0,
        SentLast24Hours = sent24h,
        FailedLast24Hours = failed24h,
        FailureRateLast7Days = Math.Round(failureRate, 2),
        TopErrors = topErrors,
        DailyVolume = dailyVolume,
    };
}
```

- [ ] **Step 7: Run all tests**

Run: `dotnet test modules/Email/tests/SimpleModule.Email.Tests/`
Expected: All tests pass.

- [ ] **Step 8: Commit**

```bash
git add modules/Email/src/SimpleModule.Email/EmailService.cs modules/Email/tests/SimpleModule.Email.Tests/
git commit -m "feat(email): add paginated queries and stats to EmailService"
```

---

### Task 8: Add audit event publishing and validation to EmailService

**Files:**
- Modify: `modules/Email/src/SimpleModule.Email/EmailService.cs`
- Modify: `modules/Email/src/SimpleModule.Email/Endpoints/Messages/SendEmailEndpoint.cs`
- Modify: `modules/Email/src/SimpleModule.Email/Endpoints/Templates/CreateTemplateEndpoint.cs`
- Modify: `modules/Email/src/SimpleModule.Email/Endpoints/Templates/UpdateTemplateEndpoint.cs`

- [ ] **Step 1: Publish audit events in EmailService template methods**

In `CreateTemplateAsync`, after `await db.SaveChangesAsync()`:

```csharp
eventBus.PublishInBackground(new EmailTemplateCreatedEvent(template.Id, template.Name, template.Slug));
```

In `UpdateTemplateAsync`, before updating fields, capture changed fields:

```csharp
var changedFields = new List<string>();
if (template.Name != request.Name) changedFields.Add("Name");
if (template.Subject != request.Subject) changedFields.Add("Subject");
if (template.Body != request.Body) changedFields.Add("Body");
if (template.IsHtml != request.IsHtml) changedFields.Add("IsHtml");
if (template.DefaultReplyTo != request.DefaultReplyTo) changedFields.Add("DefaultReplyTo");
```

After `await db.SaveChangesAsync()`:

```csharp
eventBus.PublishInBackground(new EmailTemplateUpdatedEvent(template.Id, template.Name, changedFields));
```

In `DeleteTemplateAsync`, capture name before removal, then after `await db.SaveChangesAsync()`:

```csharp
eventBus.PublishInBackground(new EmailTemplateDeletedEvent(id, template.Name));
```

- [ ] **Step 2: Add slug uniqueness check in CreateTemplateAsync**

Before creating the template entity:

```csharp
var existing = await db.EmailTemplates.AnyAsync(t => t.Slug == request.Slug);
if (existing)
{
    throw new Core.Exceptions.ConflictException($"A template with slug '{request.Slug}' already exists.");
}
```

- [ ] **Step 3: Wire validation into SendEmailEndpoint**

Update `SendEmailEndpoint.Map` to validate before calling service:

```csharp
app.MapPost(
        "/messages/send",
        async (SendEmailRequest request, IEmailContracts emailContracts) =>
        {
            var validation = SendEmailRequestValidator.Validate(request);
            if (!validation.IsValid)
                throw new Core.Exceptions.ValidationException(validation.Errors);

            var message = await emailContracts.SendEmailAsync(request);
            return TypedResults.Ok(message);
        }
    )
    .RequirePermission(EmailPermissions.Send);
```

Add the using: `using SimpleModule.Email.Validators;`

- [ ] **Step 4: Wire validation into CreateTemplateEndpoint (API)**

Update `Endpoints/Templates/CreateTemplateEndpoint.cs`:

```csharp
public void Map(IEndpointRouteBuilder app) =>
    app.MapPost(
            "/templates",
            (CreateEmailTemplateRequest request, IEmailContracts emailContracts) =>
            {
                var validation = CreateEmailTemplateRequestValidator.Validate(request);
                if (!validation.IsValid)
                    throw new Core.Exceptions.ValidationException(validation.Errors);

                return CrudEndpoints.Create(
                    () => emailContracts.CreateTemplateAsync(request),
                    t => $"/api/email/templates/{t.Id.Value}"
                );
            }
        )
        .RequirePermission(EmailPermissions.ManageTemplates);
```

Add using: `using SimpleModule.Email.Validators;`

- [ ] **Step 4b: Wire validation into UpdateTemplateEndpoint (API)**

Update `Endpoints/Templates/UpdateTemplateEndpoint.cs`:

```csharp
public void Map(IEndpointRouteBuilder app) =>
    app.MapPut(
            "/templates/{id}",
            (int id, UpdateEmailTemplateRequest request, IEmailContracts emailContracts) =>
            {
                var validation = UpdateEmailTemplateRequestValidator.Validate(request);
                if (!validation.IsValid)
                    throw new Core.Exceptions.ValidationException(validation.Errors);

                return CrudEndpoints.Update(() =>
                    emailContracts.UpdateTemplateAsync(EmailTemplateId.From(id), request)
                );
            }
        )
        .RequirePermission(EmailPermissions.ManageTemplates);
```

- [ ] **Step 4c: Wire validation into View endpoints**

Update `Views/CreateTemplateEndpoint.cs` POST handler — add validation before the `CreateTemplateAsync` call:

```csharp
app.MapPost(
        "/templates",
        async ([AsParameters] CreateTemplateForm form, IEmailContracts emailContracts) =>
        {
            var request = new CreateEmailTemplateRequest
            {
                Name = form.Name,
                Slug = form.Slug,
                Subject = form.Subject,
                Body = form.Body,
                IsHtml = form.IsHtml,
            };
            var validation = CreateEmailTemplateRequestValidator.Validate(request);
            if (!validation.IsValid)
                throw new Core.Exceptions.ValidationException(validation.Errors);

            await emailContracts.CreateTemplateAsync(request);
            return Results.Redirect("/email/templates");
        }
    )
    .RequirePermission(EmailPermissions.ManageTemplates);
```

Same pattern for `Views/EditTemplateEndpoint.cs` POST handler — validate `UpdateEmailTemplateRequest` before calling `UpdateTemplateAsync`.

- [ ] **Step 5: Run all tests**

Run: `dotnet test modules/Email/tests/SimpleModule.Email.Tests/`
Expected: All tests pass.

- [ ] **Step 6: Commit**

```bash
git add modules/Email/src/SimpleModule.Email/
git commit -m "feat(email): add audit events, slug uniqueness, and validation to endpoints"
```

---

## Chunk 4: Background Sending and Retry Jobs

### Task 9: Implement SendEmailJob and RetryFailedEmailsJob

**Files:**
- Create: `modules/Email/src/SimpleModule.Email/Jobs/SendEmailJobData.cs`
- Create: `modules/Email/src/SimpleModule.Email/Jobs/SendEmailJob.cs`
- Create: `modules/Email/src/SimpleModule.Email/Jobs/RetryFailedEmailsJob.cs`
- Modify: `modules/Email/src/SimpleModule.Email/SimpleModule.Email.csproj` (add BackgroundJobs.Contracts reference)
- Modify: `modules/Email/src/SimpleModule.Email/EmailModule.cs` (register jobs, add settings)
- Modify: `modules/Email/src/SimpleModule.Email/EmailModuleOptions.cs` (add RetryIntervalCron)
- Test: `modules/Email/tests/SimpleModule.Email.Tests/Unit/SendEmailJobTests.cs`

- [ ] **Step 1: Add BackgroundJobs.Contracts project reference to Email.csproj**

Add to `SimpleModule.Email.csproj` ItemGroup:

```xml
<ProjectReference Include="..\..\..\..\modules\BackgroundJobs\src\SimpleModule.BackgroundJobs.Contracts\SimpleModule.BackgroundJobs.Contracts.csproj" />
```

- [ ] **Step 2: Add RetryIntervalCron to EmailModuleOptions**

```csharp
public string RetryIntervalCron { get; set; } = "*/5 * * * *";
```

- [ ] **Step 3: Create SendEmailJobData**

```csharp
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Jobs;

public sealed record SendEmailJobData(EmailMessageId MessageId);
```

- [ ] **Step 4: Create SendEmailJob**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core.Events;
using SimpleModule.Email.Contracts;
using SimpleModule.Email.Contracts.Events;
using SimpleModule.Email.Providers;

namespace SimpleModule.Email.Jobs;

public partial class SendEmailJob(
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
        var message = await db.EmailMessages.FindAsync([jobData.MessageId], cancellationToken);
        if (message is null)
        {
            context.Log($"Email message {jobData.MessageId} not found, skipping.");
            return;
        }

        var opts = options.Value;
        var envelope = new EmailEnvelope(
            opts.DefaultFromAddress,
            opts.DefaultFromName,
            message.To,
            message.Cc,
            message.Bcc,
            message.ReplyTo,
            message.Subject,
            message.Body,
            message.IsHtml
        );

        try
        {
            context.Log($"Sending email {message.Id} to {message.To}");
            await emailProvider.SendAsync(envelope, cancellationToken);
            message.Status = EmailStatus.Sent;
            message.SentAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            LogEmailSent(logger, message.Id, message.To);
            eventBus.PublishInBackground(
                new EmailSentEvent(message.Id, message.To, message.Subject)
            );
        }
        catch (Exception ex)
            when (ex is InvalidOperationException or System.Net.Sockets.SocketException or IOException)
        {
            message.Status = EmailStatus.Failed;
            message.ErrorMessage = ex.Message;
            await db.SaveChangesAsync(cancellationToken);

            LogEmailFailed(logger, message.Id, message.To, ex);
            eventBus.PublishInBackground(
                new EmailFailedEvent(message.Id, message.To, message.Subject, ex.Message)
            );
        }

        context.ReportProgress(100);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Email {MessageId} sent to {To}")]
    private static partial void LogEmailSent(ILogger logger, EmailMessageId messageId, string to);

    [LoggerMessage(Level = LogLevel.Error, Message = "Email {MessageId} failed to send to {To}")]
    private static partial void LogEmailFailed(
        ILogger logger,
        EmailMessageId messageId,
        string to,
        Exception ex
    );
}
```

- [ ] **Step 5: Create RetryFailedEmailsJob**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleModule.BackgroundJobs.Contracts;
using SimpleModule.Core.Events;
using SimpleModule.Email.Contracts;
using SimpleModule.Email.Contracts.Events;

namespace SimpleModule.Email.Jobs;

public partial class RetryFailedEmailsJob(
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

            LogRetryAttempt(logger, message.Id, message.To, message.RetryCount);
            eventBus.PublishInBackground(
                new EmailRetryAttemptEvent(message.Id, message.To, message.RetryCount)
            );

            await backgroundJobs.EnqueueAsync<SendEmailJob>(
                new SendEmailJobData(message.Id),
                cancellationToken
            );
        }

        context.ReportProgress(100, $"Enqueued {failedMessages.Count} retries");
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Retrying email {MessageId} to {To} (attempt {RetryCount})"
    )]
    private static partial void LogRetryAttempt(
        ILogger logger,
        EmailMessageId messageId,
        string to,
        int retryCount
    );
}
```

- [ ] **Step 6: Update EmailService.SendEmailAsync to queue instead of send synchronously**

Replace the try/catch block with job enqueueing. The new method:

```csharp
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
```

Add `IBackgroundJobs backgroundJobs` to the `EmailService` constructor parameters. Add a new log message:

```csharp
[LoggerMessage(Level = LogLevel.Information, Message = "Email {MessageId} queued for {To}")]
private static partial void LogEmailQueued(ILogger logger, EmailMessageId messageId, string to);
```

Remove the old `HandleSendFailure` method and the direct `emailProvider.SendAsync` call from `SendEmailAsync` — that logic now lives in `SendEmailJob`.

Keep the existing `LogEmailSent` and `LogEmailFailed` in EmailService for now (they're used by log messages in other methods) or remove them if they become unused after moving to jobs. The job has its own copies.

- [ ] **Step 7: Register jobs in EmailModule.ConfigureServices**

Add to `ConfigureServices` after the provider registration:

```csharp
services.AddModuleJob<SendEmailJob>();
services.AddModuleJob<RetryFailedEmailsJob>();
```

Add the using: `using SimpleModule.BackgroundJobs.Contracts;` and `using SimpleModule.Email.Jobs;`.

Add a new setting in `ConfigureSettings`:

```csharp
.Add(
    new SettingDefinition
    {
        Key = "email.retryIntervalCron",
        DisplayName = "Retry Interval (Cron)",
        Description = "Cron expression for retrying failed emails",
        Group = "Email",
        Scope = SettingScope.System,
        DefaultValue = "*/5 * * * *",
        Type = SettingType.Text,
    }
)
```

- [ ] **Step 7b: Register recurring retry job at module startup**

The `EmailModule` needs to schedule the recurring retry job. Add a `ConfigureHost` method (or use an `IHostedService`) that registers the recurring job:

```csharp
// In EmailModule class, add IModuleStartup interface or use ConfigureHost if available
// If the module doesn't have a startup hook, create a hosted service:
```

Create `modules/Email/src/SimpleModule.Email/Jobs/EmailJobRegistrationHostedService.cs`:

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SimpleModule.BackgroundJobs.Contracts;

namespace SimpleModule.Email.Jobs;

public class EmailJobRegistrationHostedService(
    IBackgroundJobs backgroundJobs,
    IOptions<EmailModuleOptions> options
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var cron = options.Value.RetryIntervalCron;
        await backgroundJobs.AddRecurringAsync<RetryFailedEmailsJob>(
            "email-retry-failed",
            cron,
            ct: cancellationToken
        );
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

Register it in `EmailModule.ConfigureServices`:

```csharp
services.AddHostedService<EmailJobRegistrationHostedService>();
```

- [ ] **Step 8: Update EmailService tests to account for async sending**

Add a `TestBackgroundJobs` fake to the test file and update the constructor:

```csharp
private sealed class TestBackgroundJobs : IBackgroundJobs
{
    public List<(Type JobType, object? Data)> EnqueuedJobs { get; } = [];

    public Task<JobId> EnqueueAsync<TJob>(object? data = null, CancellationToken ct = default)
        where TJob : IModuleJob
    {
        EnqueuedJobs.Add((typeof(TJob), data));
        return Task.FromResult(JobId.From(Guid.NewGuid()));
    }

    public Task<JobId> ScheduleAsync<TJob>(DateTimeOffset executeAt, object? data = null, CancellationToken ct = default)
        where TJob : IModuleJob => Task.FromResult(JobId.From(Guid.NewGuid()));

    public Task<RecurringJobId> AddRecurringAsync<TJob>(string name, string cronExpression, object? data = null, CancellationToken ct = default)
        where TJob : IModuleJob => Task.FromResult(RecurringJobId.From(Guid.NewGuid()));

    public Task RemoveRecurringAsync(RecurringJobId id, CancellationToken ct = default) => Task.CompletedTask;
    public Task<bool> ToggleRecurringAsync(RecurringJobId id, CancellationToken ct = default) => Task.FromResult(true);
    public Task CancelAsync(JobId jobId, CancellationToken ct = default) => Task.CompletedTask;
    public Task<JobStatusDto?> GetStatusAsync(JobId jobId, CancellationToken ct = default) => Task.FromResult<JobStatusDto?>(null);
}
```

Update the `EmailServiceTests` constructor to pass the fake:

```csharp
private readonly TestBackgroundJobs _backgroundJobs = new();

// In constructor, update EmailService creation:
_sut = new EmailService(
    _db,
    provider,
    emailOptions,
    _eventBus,
    _backgroundJobs,
    NullLogger<EmailService>.Instance
);
```

Update existing test assertions:
- `SendEmailAsync_CreatesMessageAndSends`: Change `result.Status.Should().Be(EmailStatus.Sent)` to `result.Status.Should().Be(EmailStatus.Queued)`, remove `result.SentAt.Should().NotBeNull()` assertion
- `SendEmailAsync_PublishesEmailSentEvent`: This test now verifies a job was enqueued instead: `_backgroundJobs.EnqueuedJobs.Should().ContainSingle()` and the event is no longer published directly (it's published by the job)
- `GetAllMessagesAsync_ReturnsAllMessages`: These tests that call `SendEmailAsync` still work — they just get `Queued` messages now

- [ ] **Step 9: Build and run all tests**

Run: `dotnet test modules/Email/tests/SimpleModule.Email.Tests/`
Expected: All tests pass.

- [ ] **Step 10: Commit**

```bash
git add modules/Email/src/SimpleModule.Email/ modules/Email/tests/SimpleModule.Email.Tests/
git commit -m "feat(email): add background sending via SendEmailJob and RetryFailedEmailsJob"
```

---

## Chunk 5: API Endpoints and View Endpoints

### Task 10: Update existing endpoints and add new ones

**Files:**
- Create: `modules/Email/src/SimpleModule.Email/Endpoints/Stats/GetStatsEndpoint.cs`
- Modify: `modules/Email/src/SimpleModule.Email/Endpoints/Messages/GetAllMessagesEndpoint.cs`
- Modify: `modules/Email/src/SimpleModule.Email/Endpoints/Templates/GetAllTemplatesEndpoint.cs`
- Modify: `modules/Email/src/SimpleModule.Email/Views/HistoryEndpoint.cs`
- Modify: `modules/Email/src/SimpleModule.Email/Views/TemplatesEndpoint.cs`
- Create: `modules/Email/src/SimpleModule.Email/Views/DashboardEndpoint.cs`

- [ ] **Step 1: Update GetAllMessagesEndpoint to use QueryMessagesAsync**

```csharp
public class GetAllMessagesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/messages",
                async ([AsParameters] QueryEmailMessagesRequest request, IEmailContracts emailContracts) =>
                    TypedResults.Ok(await emailContracts.QueryMessagesAsync(request))
            )
            .RequirePermission(EmailPermissions.ViewHistory);
}
```

- [ ] **Step 2: Update GetAllTemplatesEndpoint to use QueryTemplatesAsync**

```csharp
public class GetAllTemplatesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/templates",
                async ([AsParameters] QueryEmailTemplatesRequest request, IEmailContracts emailContracts) =>
                    TypedResults.Ok(await emailContracts.QueryTemplatesAsync(request))
            )
            .RequirePermission(EmailPermissions.ViewTemplates);
}
```

- [ ] **Step 3: Create GetStatsEndpoint**

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Endpoints.Stats;

public class GetStatsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet(
                "/stats",
                async (IEmailContracts emailContracts) =>
                    TypedResults.Ok(await emailContracts.GetEmailStatsAsync())
            )
            .RequirePermission(EmailPermissions.ViewHistory);
}
```

- [ ] **Step 4: Update HistoryEndpoint to pass paginated data**

```csharp
public void Map(IEndpointRouteBuilder app)
{
    app.MapGet(
            "/history",
            async ([AsParameters] QueryEmailMessagesRequest request, IEmailContracts emailContracts) =>
                Inertia.Render(
                    "Email/History",
                    new { result = await emailContracts.QueryMessagesAsync(request), filters = request }
                )
        )
        .RequirePermission(EmailPermissions.ViewHistory);
}
```

- [ ] **Step 5: Update TemplatesEndpoint to pass paginated data**

```csharp
public void Map(IEndpointRouteBuilder app)
{
    app.MapGet(
            "/templates",
            async ([AsParameters] QueryEmailTemplatesRequest request, IEmailContracts emailContracts) =>
                Inertia.Render(
                    "Email/Templates",
                    new { result = await emailContracts.QueryTemplatesAsync(request), filters = request }
                )
        )
        .RequirePermission(EmailPermissions.ViewTemplates);
}
```

- [ ] **Step 6: Create DashboardEndpoint**

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Inertia;
using SimpleModule.Email.Contracts;

namespace SimpleModule.Email.Views;

[ViewPage("Email/Dashboard")]
public class DashboardEndpoint : IViewEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/dashboard",
                async (IEmailContracts emailContracts) =>
                    Inertia.Render(
                        "Email/Dashboard",
                        new { stats = await emailContracts.GetEmailStatsAsync() }
                    )
            )
            .RequirePermission(EmailPermissions.ViewHistory);
    }
}
```

- [ ] **Step 7: Update EmailModule menu to add Dashboard**

In `ConfigureMenu`, add before the Templates menu item:

```csharp
menus.Add(
    new MenuItem
    {
        Label = "Email Dashboard",
        Url = "/email/dashboard",
        Icon = """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"/></svg>""",
        Order = 49,
        Section = MenuSection.AdminSidebar,
    }
);
```

- [ ] **Step 8: Build to verify**

Run: `dotnet build modules/Email/src/SimpleModule.Email/SimpleModule.Email.csproj`
Expected: Build succeeded.

- [ ] **Step 9: Commit**

```bash
git add modules/Email/src/SimpleModule.Email/
git commit -m "feat(email): update endpoints for pagination and add stats/dashboard endpoints"
```

---

## Chunk 6: React Frontend Updates

### Task 11: Update TypeScript types and localization

**Files:**
- Modify: `modules/Email/src/SimpleModule.Email/types.ts` (will be regenerated by source generator on build)
- Modify: `modules/Email/src/SimpleModule.Email/Locales/en.json`
- Modify: `modules/Email/src/SimpleModule.Email/Locales/keys.ts`

- [ ] **Step 1: Build the project to regenerate types.ts**

Run: `dotnet build modules/Email/src/SimpleModule.Email/SimpleModule.Email.csproj`

This will regenerate `types.ts` with the new DTOs (QueryEmailMessagesRequest, QueryEmailTemplatesRequest, EmailStats, ErrorSummary, DailyCount, and updated fields with ReplyTo).

- [ ] **Step 2: Add the EmailStatus type alias**

Since the source generator maps enums to `any`, add a local type in `Views/History.tsx` (done in Task 12). No changes to types.ts needed beyond what the generator produces.

- [ ] **Step 3: Update en.json with dashboard and filter keys**

Add the following keys to `Locales/en.json`:

```json
{
  "Dashboard.Title": "Email Dashboard",
  "Dashboard.Description": "Monitor email delivery health and performance.",
  "Dashboard.TotalSent": "Total Sent",
  "Dashboard.TotalFailed": "Total Failed",
  "Dashboard.TotalQueued": "Queued",
  "Dashboard.TotalRetrying": "Retrying",
  "Dashboard.Last24Hours": "Last 24 hours",
  "Dashboard.FailureRate": "Failure Rate (7d)",
  "Dashboard.DailyVolume": "Daily Volume (30d)",
  "Dashboard.TopErrors": "Top Errors",
  "Dashboard.TopErrors.ErrorMessage": "Error",
  "Dashboard.TopErrors.Count": "Count",
  "Dashboard.TopErrors.Empty": "No errors recorded.",
  "Dashboard.Sent": "Sent",
  "Dashboard.Failed": "Failed",
  "History.FilterStatus": "Status",
  "History.FilterTo": "Recipient",
  "History.FilterSubject": "Subject",
  "History.FilterDateFrom": "From Date",
  "History.FilterDateTo": "To Date",
  "History.AllStatuses": "All Statuses",
  "History.Showing": "Showing",
  "History.Of": "of",
  "History.Previous": "Previous",
  "History.Next": "Next",
  "Templates.FilterSearch": "Search templates..."
}
```

Merge these into the existing en.json file (keep all existing keys, add the new ones).

- [ ] **Step 4: Update keys.ts with new keys**

Add `Dashboard` and new filter keys to `EmailKeys` in `Locales/keys.ts`. Follow the existing nested pattern.

- [ ] **Step 5: Commit**

```bash
git add modules/Email/src/SimpleModule.Email/Locales/
git commit -m "feat(email): add dashboard and filter localization keys"
```

---

### Task 12: Rewrite History.tsx with server-side pagination and filters

**Files:**
- Modify: `modules/Email/src/SimpleModule.Email/Views/History.tsx`

- [ ] **Step 1: Rewrite History.tsx**

Follow the AuditLogs Browse.tsx pattern. Key structural elements:

```tsx
// Props interface (matches server-side PagedResult)
interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

interface Props {
  result: PagedResult<EmailMessage>;
  filters: { status?: string; to?: string; subject?: string; dateFrom?: string; dateTo?: string };
}
```

Filter bar (inside PageShell, above the table):

```tsx
const [statusFilter, setStatusFilter] = useState(filters.status ?? '__all__');
const [toFilter, setToFilter] = useState(filters.to ?? '');
// ... etc for each filter

function applyFilters(overrides?: Record<string, string>) {
  const params: Record<string, string> = {};
  if (statusFilter !== '__all__') params.status = statusFilter;
  if (toFilter) params.to = toFilter;
  // ... build params from state
  router.get('/email/history', { ...params, ...overrides });
}
```

Filter bar JSX: a flex row with `Select` for status (options: All, Queued, Sending, Sent, Failed, Retrying), `Input` for recipient search, `Input` for subject search, and a `Button` to apply. Wrap in a `Card` with `CardContent`.

Pagination controls at bottom (below the table):

```tsx
const totalPages = Math.max(1, Math.ceil(result.totalCount / result.pageSize));
const currentPage = result.page;

// Navigation
function goToPage(page: number) {
  const params: Record<string, string> = { page: String(page) };
  if (statusFilter !== '__all__') params.status = statusFilter;
  if (toFilter) params.to = toFilter;
  // ... preserve current filters
  router.get('/email/history', params);
}
```

Pagination JSX: "Showing X-Y of Z" text, Previous/Next buttons (disabled at boundaries), page number buttons for a window around current page.

Table structure same as current but using `result.items` instead of `messages`, wrapped in a conditional for empty state.

- [ ] **Step 2: Run lint check**

Run: `npx biome check modules/Email/src/SimpleModule.Email/Views/History.tsx`
Expected: No errors.

- [ ] **Step 3: Commit**

```bash
git add modules/Email/src/SimpleModule.Email/Views/History.tsx
git commit -m "feat(email): add server-side pagination and filters to History view"
```

---

### Task 13: Rewrite Templates.tsx with server-side pagination and search

**Files:**
- Modify: `modules/Email/src/SimpleModule.Email/Views/Templates.tsx`

- [ ] **Step 1: Rewrite Templates.tsx**

Props and filter structure:

```tsx
interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

interface Props {
  result: PagedResult<EmailTemplate>;
  filters: { search?: string };
}
```

Filter bar: single `Input` for search with `onKeyDown` handler that calls `router.get('/email/templates', { search })` on Enter. Place in the `filterBar` prop of `DataGridPage` or above the grid inside a `PageShell`.

Pagination: same pattern as History.tsx — `goToPage(page)` calls `router.get('/email/templates', { search, page: String(page) })`. Show Previous/Next + page numbers below the table.

Data binding: use `result.items` for `data` prop on `DataGridPage`, `result.totalCount` for pagination math.

Keep the existing delete dialog unchanged. Keep all `useTranslation` keys from the current version.

- [ ] **Step 2: Run lint check**

Run: `npx biome check modules/Email/src/SimpleModule.Email/Views/Templates.tsx`
Expected: No errors.

- [ ] **Step 3: Commit**

```bash
git add modules/Email/src/SimpleModule.Email/Views/Templates.tsx
git commit -m "feat(email): add server-side pagination and search to Templates view"
```

---

### Task 14: Create Dashboard.tsx

**Files:**
- Create: `modules/Email/src/SimpleModule.Email/Views/Dashboard.tsx`
- Modify: `modules/Email/src/SimpleModule.Email/Pages/index.ts`

- [ ] **Step 1: Create Dashboard.tsx**

The dashboard view should include:
- `PageShell` with title/description from translations
- 4 summary cards in a grid: Total Sent, Total Failed, Queued, Retrying — each showing the count and a "(X in last 24h)" subtitle for sent/failed
- Failure rate badge: green (`< 5%`), yellow (`5-15%`), red (`> 15%`) using Badge from `@simplemodule/ui`
- Daily volume bar chart using `ChartContainer` + recharts `BarChart` with `Bar` for sent (primary color) and failed (danger color)
- Top errors table: simple Table with ErrorMessage and Count columns, with empty state text

Use `useTranslation('Email')` with `EmailKeys.Dashboard.*` keys.

Props type: `{ stats: EmailStats }` where `EmailStats` matches the generated type.

- [ ] **Step 2: Add Dashboard to Pages/index.ts**

Add entry:

```typescript
'Email/Dashboard': () => import('../Views/Dashboard'),
```

- [ ] **Step 3: Run lint check**

Run: `npx biome check modules/Email/src/SimpleModule.Email/Views/Dashboard.tsx modules/Email/src/SimpleModule.Email/Pages/index.ts`
Expected: No errors.

- [ ] **Step 4: Build the full module**

Run: `dotnet build modules/Email/src/SimpleModule.Email/SimpleModule.Email.csproj`
Expected: Build succeeded (includes Vite build of frontend).

- [ ] **Step 5: Commit**

```bash
git add modules/Email/src/SimpleModule.Email/Views/Dashboard.tsx modules/Email/src/SimpleModule.Email/Pages/index.ts
git commit -m "feat(email): add statistics dashboard view with charts"
```

---

## Chunk 7: Final Verification

### Task 15: Run full test suite and verify build

- [ ] **Step 1: Run all Email tests**

Run: `dotnet test modules/Email/tests/SimpleModule.Email.Tests/`
Expected: All tests pass.

- [ ] **Step 2: Run full solution build**

Run: `dotnet build`
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Run frontend lint**

Run: `npx biome check modules/Email/src/SimpleModule.Email/`
Expected: No errors.

- [ ] **Step 4: Validate page registry**

Run: `npm run validate-pages`
Expected: No mismatches (all view endpoints have corresponding page entries).

- [ ] **Step 5: Commit any final fixes**

If any issues were found in steps 1-4, fix and commit.
