# AI & Agents Architecture Redesign — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restructure the AI/agents framework from 10 flat packages with 3 competing extension mechanisms into 2 framework packages with a single hook-based extension point, moving all features into modules with config-based feature flags.

**Architecture:** Thin core (`IAgentChatHook` in Core, slim `AgentChatService` in Agents framework) + features in modules (Agents module owns sessions/guardrails/rate-limiting/telemetry, Rag module owns vector search/StructuredRag). Single `SimpleModule.AI` package merges all 4 providers.

**Tech Stack:** .NET 10, Microsoft.Extensions.AI, Roslyn incremental generators, EF Core, xUnit

**Spec:** `docs/superpowers/specs/2026-04-02-ai-agents-redesign.md`

---

## Chunk 1: Core Interfaces & Hook System

### Task 1: Add IAgentChatHook and AgentChatContext to Core

**Files:**
- Create: `framework/SimpleModule.Core/Agents/IAgentChatHook.cs`
- Create: `framework/SimpleModule.Core/Agents/AgentChatHookBase.cs`
- Create: `framework/SimpleModule.Core/Agents/AgentChatContext.cs`
- Create: `framework/SimpleModule.Core/Agents/IAgentChatService.cs`

- [ ] **Step 1: Create IAgentChatHook interface**

```csharp
// framework/SimpleModule.Core/Agents/IAgentChatHook.cs
namespace SimpleModule.Core.Agents;

/// <summary>
/// Single extension point for agent chat pipeline. Implementations are discovered
/// from DI and run in Order (ascending for Before, descending for After).
/// Order ranges: 0-99 security, 100-199 context enrichment, 200-299 observability.
/// </summary>
public interface IAgentChatHook
{
    int Order { get; }
    Task OnBeforeChatAsync(AgentChatContext context, CancellationToken ct);
    Task OnAfterChatAsync(AgentChatContext context, CancellationToken ct);
}
```

- [ ] **Step 2: Create AgentChatHookBase with no-op defaults**

```csharp
// framework/SimpleModule.Core/Agents/AgentChatHookBase.cs
namespace SimpleModule.Core.Agents;

/// <summary>
/// Base class for hooks that only need one side (Before or After).
/// </summary>
public abstract class AgentChatHookBase : IAgentChatHook
{
    public abstract int Order { get; }
    public virtual Task OnBeforeChatAsync(AgentChatContext context, CancellationToken ct) => Task.CompletedTask;
    public virtual Task OnAfterChatAsync(AgentChatContext context, CancellationToken ct) => Task.CompletedTask;
}
```

- [ ] **Step 3: Create AgentChatContext**

```csharp
// framework/SimpleModule.Core/Agents/AgentChatContext.cs
using System.Security.Claims;
using Microsoft.Extensions.AI;

namespace SimpleModule.Core.Agents;

/// <summary>
/// Context bag that flows through the hook pipeline. Hooks can modify Messages,
/// ChatOptions, set IsRejected, or store cross-hook state in Properties.
/// </summary>
public sealed class AgentChatContext
{
    public AgentChatContext(
        IAgentDefinition agentDefinition,
        string message,
        string? sessionId,
        ClaimsPrincipal? user)
    {
        AgentDefinition = agentDefinition;
        Message = message;
        SessionId = sessionId ?? Guid.NewGuid().ToString();
        User = user;
        Messages = [];
        ChatOptions = new ChatOptions();
        Properties = new Dictionary<string, object>();
        StartedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Resolved agent definition instance (Instructions, EnableRag, MaxTokens, etc.).</summary>
    public IAgentDefinition AgentDefinition { get; }
    /// <summary>The original user message.</summary>
    public string Message { get; }
    public string SessionId { get; }
    public ClaimsPrincipal? User { get; }
    public List<ChatMessage> Messages { get; }
    public ChatOptions ChatOptions { get; }
    public string? ResponseText { get; set; }
    public bool IsRejected { get; set; }
    public string? RejectionReason { get; set; }
    /// <summary>Cross-hook state. Well-known keys: "AgentRegistration", "Activity".</summary>
    public Dictionary<string, object> Properties { get; }
    public DateTimeOffset StartedAt { get; }
}
```

Note: This uses `Microsoft.Extensions.AI` types (`ChatMessage`, `ChatOptions`). The `SimpleModule.Core.csproj` will need a new `<PackageReference Include="Microsoft.Extensions.AI.Abstractions" />`.

The `AgentChatContext` uses `IAgentDefinition` (Core type) as its primary agent accessor. Hooks that need `AgentRegistration` metadata (e.g., `ModuleName`) can access it via `context.Properties["AgentRegistration"]` — the `AgentChatService` stores it there during context creation.

- [ ] **Step 4: Create IAgentChatService interface**

```csharp
// framework/SimpleModule.Core/Agents/IAgentChatService.cs
using System.Security.Claims;
using SimpleModule.Agents.Dtos;

namespace SimpleModule.Core.Agents;

public interface IAgentChatService
{
    Task<AgentChatResult> ChatAsync(
        string agentName, AgentChatRequest request, ClaimsPrincipal? user, CancellationToken ct);

    IAsyncEnumerable<AgentChatStreamChunk> ChatStreamAsync(
        string agentName, AgentChatRequest request, ClaimsPrincipal? user, CancellationToken ct);
}

public sealed record AgentChatResult(string Message, string SessionId, bool IsRejected = false, string? RejectionReason = null)
{
    public static AgentChatResult Rejected(string? reason) => new("", "", true, reason);
}

public sealed record AgentChatStreamChunk(string Text, bool IsComplete);
```

Note: `IAgentChatService` references `AgentChatRequest` from the Agents package. Since Core can't depend on Agents, move the `AgentChatRequest` record to Core (`SimpleModule.Core.Agents.AgentChatRequest`) and have the Agents package re-export or reference it. Alternatively, keep the interface in the Agents package instead of Core. The pragmatic choice: keep `IAgentChatService` in the **Agents framework package** (not Core) since it's part of the runtime, and Core only holds the hook/context contracts.

- [ ] **Step 5: Add Microsoft.Extensions.AI.Abstractions to Core csproj**

Modify: `framework/SimpleModule.Core/SimpleModule.Core.csproj` — add to `<ItemGroup>`:
```xml
<PackageReference Include="Microsoft.Extensions.AI.Abstractions" />
```

- [ ] **Step 6: Build to verify compilation**

Run: `dotnet build framework/SimpleModule.Core/SimpleModule.Core.csproj`
Expected: Build succeeded

- [ ] **Step 7: Commit**

```bash
git add framework/SimpleModule.Core/Agents/IAgentChatHook.cs \
       framework/SimpleModule.Core/Agents/AgentChatHookBase.cs \
       framework/SimpleModule.Core/Agents/AgentChatContext.cs \
       framework/SimpleModule.Core/Agents/IAgentChatService.cs \
       framework/SimpleModule.Core/SimpleModule.Core.csproj
git commit -m "feat: add IAgentChatHook, AgentChatContext, and IAgentChatService to Core"
```

### Task 2: Remove IAgentBuilder and ConfigureAgents from Core

**Files:**
- Delete: `framework/SimpleModule.Core/Agents/IAgentBuilder.cs`
- Modify: `framework/SimpleModule.Core/IModule.cs`

- [ ] **Step 1: Delete IAgentBuilder.cs**

```bash
rm framework/SimpleModule.Core/Agents/IAgentBuilder.cs
```

- [ ] **Step 2: Remove ConfigureAgents from IModule**

In `framework/SimpleModule.Core/IModule.cs`, remove line 23:
```csharp
virtual void ConfigureAgents(IAgentBuilder builder) { }
```
Also remove the `using SimpleModule.Core.Agents;` import if no other agent types are referenced (they aren't — the remaining Core/Agents types use their own namespace).

- [ ] **Step 3: Build Core to verify**

Run: `dotnet build framework/SimpleModule.Core/SimpleModule.Core.csproj`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add -u framework/SimpleModule.Core/
git commit -m "refactor: remove IAgentBuilder and ConfigureAgents from IModule"
```

---

## Chunk 2: Merge AI Providers into SimpleModule.AI

### Task 3: Create SimpleModule.AI unified package

**Files:**
- Create: `framework/SimpleModule.AI/SimpleModule.AI.csproj`
- Create: `framework/SimpleModule.AI/SimpleModuleAIExtensions.cs`
- Create: `framework/SimpleModule.AI/AIOptions.cs`
- Create: `framework/SimpleModule.AI/Providers/OpenAIProvider.cs`
- Create: `framework/SimpleModule.AI/Providers/AnthropicProvider.cs`
- Create: `framework/SimpleModule.AI/Providers/AzureOpenAIProvider.cs`
- Create: `framework/SimpleModule.AI/Providers/OllamaProvider.cs`

- [ ] **Step 1: Create the csproj with all provider dependencies**

```xml
<!-- framework/SimpleModule.AI/SimpleModule.AI.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Library</OutputType>
    <Description>Unified AI provider registration for SimpleModule. Supports OpenAI, Anthropic, Azure OpenAI, and Ollama.</Description>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="OpenAI" />
    <PackageReference Include="Microsoft.Extensions.AI.OpenAI" />
    <PackageReference Include="Anthropic.SDK" />
    <PackageReference Include="Azure.AI.OpenAI" />
    <PackageReference Include="Microsoft.Extensions.AI.Ollama" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create AIOptions with per-provider config**

```csharp
// framework/SimpleModule.AI/AIOptions.cs
namespace SimpleModule.AI;

public sealed class AIOptions
{
    public string Provider { get; set; } = "";
}

public sealed class OpenAIOptions
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gpt-4o";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
}

public sealed class AnthropicOptions
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "claude-sonnet-4-20250514";
}

public sealed class AzureOpenAIOptions
{
    public string Endpoint { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string DeploymentName { get; set; } = "";
    public string EmbeddingDeploymentName { get; set; } = "";
}

public sealed class OllamaOptions
{
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
}
```

- [ ] **Step 3: Create provider registration classes**

Each provider file follows the same pattern as the existing extensions. Move the logic from `OpenAIExtensions.cs`, `AnthropicExtensions.cs`, `AzureOpenAIExtensions.cs`, `OllamaExtensions.cs` into `Providers/` directory, keeping internal static methods:

```csharp
// framework/SimpleModule.AI/Providers/OpenAIProvider.cs
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;

namespace SimpleModule.AI.Providers;

internal static class OpenAIProvider
{
    public static void Register(IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<OpenAIOptions>>().Value;
            return new OpenAIClient(opts.ApiKey);
        });
        services.AddSingleton<IChatClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<OpenAIOptions>>().Value;
            return sp.GetRequiredService<OpenAIClient>().GetChatClient(opts.Model).AsIChatClient();
        });
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<OpenAIOptions>>().Value;
            return sp.GetRequiredService<OpenAIClient>()
                .GetEmbeddingClient(opts.EmbeddingModel)
                .AsIEmbeddingGenerator();
        });
    }
}
```

Create `AnthropicProvider.cs`, `AzureOpenAIProvider.cs`, `OllamaProvider.cs` following the same pattern — directly porting the existing extension code into static `Register` methods.

- [ ] **Step 4: Create unified registration entry point**

```csharp
// framework/SimpleModule.AI/SimpleModuleAIExtensions.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.AI.Providers;

namespace SimpleModule.AI;

public static class SimpleModuleAIExtensions
{
    public static IServiceCollection AddSimpleModuleAI(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>("AI:Provider") ?? "";

        switch (provider.ToUpperInvariant())
        {
            case "OPENAI":
                services.Configure<OpenAIOptions>(configuration.GetSection("AI:OpenAI"));
                OpenAIProvider.Register(services);
                break;
            case "ANTHROPIC":
                services.Configure<AnthropicOptions>(configuration.GetSection("AI:Anthropic"));
                AnthropicProvider.Register(services);
                break;
            case "AZUREOPENAI":
                services.Configure<AzureOpenAIOptions>(configuration.GetSection("AI:AzureOpenAI"));
                AzureOpenAIProvider.Register(services);
                break;
            case "OLLAMA":
                services.Configure<OllamaOptions>(configuration.GetSection("AI:Ollama"));
                OllamaProvider.Register(services);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unknown AI provider '{provider}'. Set AI:Provider to one of: OpenAI, Anthropic, AzureOpenAI, Ollama");
        }

        return services;
    }

    public static IServiceCollection AddSimpleModuleAI(
        this IServiceCollection services,
        Action<AIBuilder> configure)
    {
        var builder = new AIBuilder(services);
        configure(builder);
        return services;
    }
}

public sealed class AIBuilder(IServiceCollection services)
{
    public AIBuilder UseOpenAI(IConfiguration configuration)
    {
        services.Configure<OpenAIOptions>(configuration.GetSection("AI:OpenAI"));
        OpenAIProvider.Register(services);
        return this;
    }

    public AIBuilder UseAnthropic(IConfiguration configuration)
    {
        services.Configure<AnthropicOptions>(configuration.GetSection("AI:Anthropic"));
        AnthropicProvider.Register(services);
        return this;
    }

    public AIBuilder UseAzureOpenAI(IConfiguration configuration)
    {
        services.Configure<AzureOpenAIOptions>(configuration.GetSection("AI:AzureOpenAI"));
        AzureOpenAIProvider.Register(services);
        return this;
    }

    public AIBuilder UseOllama(IConfiguration configuration)
    {
        services.Configure<OllamaOptions>(configuration.GetSection("AI:Ollama"));
        OllamaProvider.Register(services);
        return this;
    }
}
```

- [ ] **Step 5: Add to solution file**

Add to `SimpleModule.slnx` under `/framework/`:
```xml
<Project Path="framework/SimpleModule.AI/SimpleModule.AI.csproj" />
```

- [ ] **Step 6: Build to verify**

Run: `dotnet build framework/SimpleModule.AI/SimpleModule.AI.csproj`
Expected: Build succeeded

- [ ] **Step 7: Commit**

```bash
git add framework/SimpleModule.AI/
git commit -m "feat: create unified SimpleModule.AI package merging all 4 providers"
```

---

## Chunk 3: Slim Down SimpleModule.Agents Framework Package

### Task 4: Rewrite AgentChatService with hook pipeline

**Files:**
- Modify: `framework/SimpleModule.Agents/AgentChatService.cs`
- Modify: `framework/SimpleModule.Agents/Dtos/AgentChatRequest.cs` (remove ResponseType)
- Modify: `framework/SimpleModule.Agents/Dtos/AgentChatResponse.cs`

- [ ] **Step 1: Remove ResponseType from AgentChatRequest**

Replace `framework/SimpleModule.Agents/Dtos/AgentChatRequest.cs`:
```csharp
namespace SimpleModule.Agents.Dtos;

public sealed record AgentChatRequest(
    string Message,
    string? SessionId = null
);
```

- [ ] **Step 2: Rewrite AgentChatService with hooks**

Replace `framework/SimpleModule.Agents/AgentChatService.cs` entirely:

```csharp
using System.Runtime.CompilerServices;
using System.Security.Claims;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Agents;

namespace SimpleModule.Agents;

public sealed class AgentChatService(
    IAgentRegistry registry,
    IChatClient chatClient,
    IServiceProvider serviceProvider,
    IEnumerable<IAgentChatHook> hooks,
    IOptions<AgentOptions> agentOptions,
    ILogger<AgentChatService> logger
) : IAgentChatService
{
    private readonly IReadOnlyList<IAgentChatHook> _orderedHooks =
        hooks.OrderBy(h => h.Order).ToList();

    public async Task<AgentChatResult> ChatAsync(
        string agentName, AgentChatRequest request,
        ClaimsPrincipal? user, CancellationToken ct)
    {
        var context = BuildContext(agentName, request, user);

        // Before hooks
        foreach (var hook in _orderedHooks)
        {
            await hook.OnBeforeChatAsync(context, ct);
            if (context.IsRejected)
                return AgentChatResult.Rejected(context.RejectionReason);
        }

        // LLM call
        var response = await chatClient.GetResponseAsync(context.Messages, context.ChatOptions, ct);
        context.ResponseText = response.Text ?? "";

        // After hooks (reverse order, each in try/catch)
        foreach (var hook in _orderedHooks.Reverse())
        {
            try
            {
                await hook.OnAfterChatAsync(context, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "After-hook {Hook} failed for agent {Agent}",
                    hook.GetType().Name, agentName);
            }
        }

        return new AgentChatResult(context.ResponseText, context.SessionId);
    }

    public async IAsyncEnumerable<AgentChatStreamChunk> ChatStreamAsync(
        string agentName, AgentChatRequest request,
        ClaimsPrincipal? user, [EnumeratorCancellation] CancellationToken ct)
    {
        var context = BuildContext(agentName, request, user);

        // Before hooks
        foreach (var hook in _orderedHooks)
        {
            await hook.OnBeforeChatAsync(context, ct);
            if (context.IsRejected)
            {
                yield return new AgentChatStreamChunk(context.RejectionReason ?? "Rejected", true);
                yield break;
            }
        }

        // Stream LLM call, accumulate full response
        var accumulated = new System.Text.StringBuilder();
        await foreach (var update in chatClient.GetStreamingResponseAsync(
            context.Messages, context.ChatOptions, ct))
        {
            foreach (var content in update.Contents)
            {
                if (content is TextContent textContent && textContent.Text is not null)
                {
                    accumulated.Append(textContent.Text);
                    yield return new AgentChatStreamChunk(textContent.Text, false);
                }
            }
        }

        yield return new AgentChatStreamChunk("", true);

        // After hooks with accumulated response
        context.ResponseText = accumulated.ToString();
        foreach (var hook in _orderedHooks.Reverse())
        {
            try
            {
                await hook.OnAfterChatAsync(context, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "After-hook {Hook} failed for agent {Agent}",
                    hook.GetType().Name, agentName);
            }
        }
    }

    private AgentChatContext BuildContext(
        string agentName, AgentChatRequest request, ClaimsPrincipal? user)
    {
        var registration = registry.GetByName(agentName)
            ?? throw new InvalidOperationException($"Agent '{agentName}' not found");

        var agentDef = (IAgentDefinition)
            ActivatorUtilities.CreateInstance(serviceProvider, registration.AgentDefinitionType);

        var context = new AgentChatContext(agentDef, request.Message, request.SessionId, user);
        context.Properties["AgentRegistration"] = registration;

        // System message
        context.Messages.Add(new ChatMessage(ChatRole.System, agentDef.Instructions));
        context.Messages.Add(new ChatMessage(ChatRole.User, message));

        // Resolve tools from tool providers
        var tools = new List<AITool>();
        foreach (var providerType in registration.ToolProviderTypes)
        {
            var provider = (IAgentToolProvider)
                ActivatorUtilities.CreateInstance(serviceProvider, providerType);
            var methods = providerType.GetMethods()
                .Where(m => m.GetCustomAttributes(typeof(AgentToolAttribute), false).Length > 0);
            foreach (var method in methods)
                tools.Add(AIFunctionFactory.Create(method, provider));
        }

        var opts = agentOptions.Value;
        context.ChatOptions.MaxOutputTokens = agentDef.MaxTokens ?? opts.MaxTokens;
        context.ChatOptions.Temperature = agentDef.Temperature ?? opts.Temperature;
        context.ChatOptions.Tools = tools.Count > 0 ? tools : null;

        return context;
    }
}
```

- [ ] **Step 3: Update AgentEndpoints to use IAgentChatService**

Replace `framework/SimpleModule.Agents/AgentEndpoints.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Agents.Dtos;
using SimpleModule.Core.Agents;

namespace SimpleModule.Agents;

public static class AgentEndpoints
{
    public static void MapAgentEndpoints(IEndpointRouteBuilder app, IAgentRegistry registry)
    {
        var group = app.MapGroup("/api/agents").WithTags("Agents").RequireAuthorization();

        group.MapGet("/", (IAgentRegistry reg) =>
            reg.GetAll().Select(a => new AgentInfo(a.Name, a.Description, a.ModuleName)));

        group.MapPost("/{name}/chat", async (
            string name,
            AgentChatRequest request,
            IAgentChatService service,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var result = await service.ChatAsync(name, request, httpContext.User, ct);
            return result.IsRejected
                ? Results.Json(new { error = result.RejectionReason }, statusCode: 422)
                : Results.Ok(new AgentChatResponse(result.Message, result.SessionId));
        });

        group.MapPost("/{name}/chat/stream", async (
            string name,
            AgentChatRequest request,
            IAgentChatService service,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            httpContext.Response.ContentType = "text/event-stream";
            httpContext.Response.Headers.CacheControl = "no-cache";
            httpContext.Response.Headers.Connection = "keep-alive";

            await foreach (var chunk in service.ChatStreamAsync(
                name, request, httpContext.User, ct))
            {
                var data = System.Text.Json.JsonSerializer.Serialize(
                    new { text = chunk.Text, done = chunk.IsComplete });
                await httpContext.Response.WriteAsync($"data: {data}\n\n", ct);
                await httpContext.Response.Body.FlushAsync(ct);
            }

            await httpContext.Response.WriteAsync("data: [DONE]\n\n", ct);
        });
    }
}
```

- [ ] **Step 4: Build to check compilation**

Run: `dotnet build framework/SimpleModule.Agents/SimpleModule.Agents.csproj`
Expected: May fail — we haven't cleaned up the DI registration yet. That's next.

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Agents/AgentChatService.cs \
       framework/SimpleModule.Agents/AgentEndpoints.cs \
       framework/SimpleModule.Agents/Dtos/AgentChatRequest.cs
git commit -m "refactor: rewrite AgentChatService with hook pipeline, remove god-class"
```

### Task 5: Clean up SimpleModuleAgentExtensions and delete dead code

**Files:**
- Modify: `framework/SimpleModule.Agents/SimpleModuleAgentExtensions.cs`
- Modify: `framework/SimpleModule.Agents/SimpleModule.Agents.csproj` (remove Rag + Storage references)
- Delete: `framework/SimpleModule.Agents/Middleware/` (entire directory — 7 files)
- Delete: `framework/SimpleModule.Agents/Guardrails/` (entire directory — 6 files)
- Delete: `framework/SimpleModule.Agents/Files/AgentFileService.cs`
- Delete: `framework/SimpleModule.Agents/Events/` (entire directory — 3 files)
- Delete: `framework/SimpleModule.Agents/Telemetry/AgentActivitySource.cs`
- Delete: `framework/SimpleModule.Agents/DevTools/AgentPlaygroundEndpoints.cs`
- Delete: `framework/SimpleModule.Agents/Sessions/` (entire directory — 4 files)
- Delete: `framework/SimpleModule.Agents/AgentBuilder.cs`
- Delete: `framework/SimpleModule.Agents/AgentSettingsDefinitions.cs`
- Modify: `framework/SimpleModule.Agents/AgentOptions.cs` (slim down)

- [ ] **Step 1: Delete all dead code directories**

```bash
rm -rf framework/SimpleModule.Agents/Middleware/
rm -rf framework/SimpleModule.Agents/Guardrails/
rm -rf framework/SimpleModule.Agents/Files/
rm -rf framework/SimpleModule.Agents/Events/
rm -rf framework/SimpleModule.Agents/Telemetry/
rm -rf framework/SimpleModule.Agents/DevTools/
rm -rf framework/SimpleModule.Agents/Sessions/
rm framework/SimpleModule.Agents/AgentBuilder.cs
rm framework/SimpleModule.Agents/AgentSettingsDefinitions.cs
```

- [ ] **Step 2: Slim down AgentOptions**

Replace `framework/SimpleModule.Agents/AgentOptions.cs`:
```csharp
namespace SimpleModule.Agents;

public sealed class AgentOptions
{
    public bool Enabled { get; set; } = true;
    public int MaxTokens { get; set; } = 4096;
    public float Temperature { get; set; } = 0.7f;
    public bool EnableStreaming { get; set; } = true;
}
```

Removed: `EnableRag` (Rag module's concern), `SessionTimeout` (Agents module's concern), `RateLimit` (Agents module's concern). Also delete `AgentRateLimitOptions` class.

- [ ] **Step 3: Rewrite SimpleModuleAgentExtensions**

Replace `framework/SimpleModule.Agents/SimpleModuleAgentExtensions.cs`:
```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core.Agents;

namespace SimpleModule.Agents;

public static class SimpleModuleAgentExtensions
{
    public static IServiceCollection AddSimpleModuleAgents(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<AgentOptions>? configure = null)
    {
        services.Configure<AgentOptions>(configuration.GetSection("Agents"));
        if (configure is not null)
            services.PostConfigure(configure);

        services.AddScoped<IAgentChatService, AgentChatService>();

        return services;
    }
}
```

- [ ] **Step 4: Update csproj — remove Rag and Storage references**

Replace `framework/SimpleModule.Agents/SimpleModule.Agents.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Library</OutputType>
    <Description>Slim AI Agent runtime for SimpleModule. Provides agent registry, hook-based chat service, and REST endpoints.</Description>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="Microsoft.Extensions.AI" />
    <ProjectReference Include="..\SimpleModule.Core\SimpleModule.Core.csproj" />
  </ItemGroup>
</Project>
```

Note: Changed from `Microsoft.Extensions.AI.Abstractions` to `Microsoft.Extensions.AI` (the full package) because `AgentChatService` uses `AIFunctionFactory.Create()` which is in the non-abstractions package.

- [ ] **Step 5: Build to verify**

Run: `dotnet build framework/SimpleModule.Agents/SimpleModule.Agents.csproj`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add -A framework/SimpleModule.Agents/
git commit -m "refactor: strip SimpleModule.Agents to slim core — remove middleware, guardrails, sessions, dead code"
```

---

## Chunk 4: Refactor Agents Module — Hook-Based Features

### Task 6: Add GuardrailHook to Agents module

**Files:**
- Create: `modules/Agents/src/SimpleModule.Agents.Module/Hooks/GuardrailHook.cs`
- Create: `modules/Agents/src/SimpleModule.Agents.Module/Options/GuardrailOptions.cs`

- [ ] **Step 1: Create GuardrailOptions**

```csharp
// modules/Agents/src/SimpleModule.Agents.Module/Options/GuardrailOptions.cs
namespace SimpleModule.Agents.Module.Options;

public sealed class GuardrailOptions
{
    public bool Enabled { get; set; } = true;
    public int MaxInputLength { get; set; } = 10_000;
    public int MaxOutputLength { get; set; } = 50_000;
    public bool PiiRedaction { get; set; } = true;
    public bool PromptInjectionDetection { get; set; } = true;
}
```

- [ ] **Step 2: Create GuardrailHook consolidating all 3 guardrails**

```csharp
// modules/Agents/src/SimpleModule.Agents.Module/Hooks/GuardrailHook.cs
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using SimpleModule.Agents.Module.Options;
using SimpleModule.Core.Agents;

namespace SimpleModule.Agents.Module.Hooks;

public sealed partial class GuardrailHook(IOptions<GuardrailOptions> options) : AgentChatHookBase
{
    public override int Order => 10;

    public override Task OnBeforeChatAsync(AgentChatContext context, CancellationToken ct)
    {
        var opts = options.Value;

        // Content length check
        if (context.Message.Length > opts.MaxInputLength)
        {
            context.IsRejected = true;
            context.RejectionReason = $"Input exceeds maximum length of {opts.MaxInputLength} characters";
            return Task.CompletedTask;
        }

        // Prompt injection detection
        if (opts.PromptInjectionDetection && PromptInjectionPattern().IsMatch(context.Message))
        {
            context.IsRejected = true;
            context.RejectionReason = "Input contains potentially harmful prompt injection patterns";
            return Task.CompletedTask;
        }

        // PII redaction on input (sanitize, don't reject)
        if (opts.PiiRedaction)
        {
            // PII patterns are applied to messages already in context
            // Input sanitization happens at message level
        }

        return Task.CompletedTask;
    }

    public override Task OnAfterChatAsync(AgentChatContext context, CancellationToken ct)
    {
        var opts = options.Value;
        if (context.ResponseText is null) return Task.CompletedTask;

        // Output length check
        if (context.ResponseText.Length > opts.MaxOutputLength)
        {
            context.ResponseText = context.ResponseText[..opts.MaxOutputLength];
        }

        // PII redaction on output
        if (opts.PiiRedaction)
        {
            context.ResponseText = EmailPattern().Replace(context.ResponseText, "[EMAIL_REDACTED]");
            context.ResponseText = PhonePattern().Replace(context.ResponseText, "[PHONE_REDACTED]");
            context.ResponseText = SsnPattern().Replace(context.ResponseText, "[SSN_REDACTED]");
        }

        return Task.CompletedTask;
    }

    [GeneratedRegex(@"(?i)(ignore\s+(all\s+)?previous\s+instructions|system\s*prompt\s*:|new\s+instructions\s*:)", RegexOptions.Compiled)]
    private static partial Regex PromptInjectionPattern();

    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex PhonePattern();

    [GeneratedRegex(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex SsnPattern();
}
```

- [ ] **Step 3: Commit**

```bash
git add modules/Agents/src/SimpleModule.Agents.Module/Hooks/GuardrailHook.cs \
       modules/Agents/src/SimpleModule.Agents.Module/Options/GuardrailOptions.cs
git commit -m "feat: add GuardrailHook consolidating content length, PII, and injection checks"
```

### Task 7: Add SessionHistoryHook to Agents module

**Files:**
- Create: `modules/Agents/src/SimpleModule.Agents.Module/Hooks/SessionHistoryHook.cs`
- Create: `modules/Agents/src/SimpleModule.Agents.Module/Options/SessionOptions.cs`
- Keep: `modules/Agents/src/SimpleModule.Agents.Module/EfAgentSessionStore.cs` (already exists)

The session store interfaces (`IAgentSessionStore`, `AgentSession`, `AgentMessage`) need to move from the deleted `framework/SimpleModule.Agents/Sessions/` into the Agents module.

- [ ] **Step 1: Move session types into the Agents module**

Create: `modules/Agents/src/SimpleModule.Agents.Module/Sessions/IAgentSessionStore.cs`
Create: `modules/Agents/src/SimpleModule.Agents.Module/Sessions/AgentSession.cs`
Create: `modules/Agents/src/SimpleModule.Agents.Module/Sessions/AgentMessage.cs`
Create: `modules/Agents/src/SimpleModule.Agents.Module/Sessions/InMemoryAgentSessionStore.cs`

Copy these from the deleted framework files, updating the namespace from `SimpleModule.Agents.Sessions` to `SimpleModule.Agents.Module.Sessions`.

- [ ] **Step 2: Create SessionOptions**

```csharp
// modules/Agents/src/SimpleModule.Agents.Module/Options/SessionOptions.cs
namespace SimpleModule.Agents.Module.Options;

public sealed class SessionOptions
{
    public bool Enabled { get; set; } = true;
    public int MaxHistory { get; set; } = 50;
    public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromMinutes(30);
}
```

- [ ] **Step 3: Create SessionHistoryHook**

```csharp
// modules/Agents/src/SimpleModule.Agents.Module/Hooks/SessionHistoryHook.cs
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using SimpleModule.Agents.Module.Options;
using SimpleModule.Agents.Module.Sessions;
using SimpleModule.Core.Agents;

namespace SimpleModule.Agents.Module.Hooks;

public sealed class SessionHistoryHook(
    IAgentSessionStore sessionStore,
    IOptions<SessionOptions> options
) : AgentChatHookBase
{
    public override int Order => 100;

    public override async Task OnBeforeChatAsync(AgentChatContext context, CancellationToken ct)
    {
        var session = await sessionStore.GetSessionAsync(context.SessionId, ct);
        if (session is null)
        {
            await sessionStore.CreateSessionAsync(
                context.AgentName, context.User?.Identity?.Name, ct);
        }

        // Load conversation history
        var history = await sessionStore.GetHistoryAsync(
            context.SessionId, options.Value.MaxHistory, ct);

        // Insert history after system message, before user message
        var insertIndex = 1; // after system prompt
        foreach (var msg in history)
        {
            var role = msg.Role == "assistant" ? ChatRole.Assistant : ChatRole.User;
            context.Messages.Insert(insertIndex++, new ChatMessage(role, msg.Content));
        }
    }

    public override async Task OnAfterChatAsync(AgentChatContext context, CancellationToken ct)
    {
        // Save user message
        await sessionStore.SaveMessageAsync(context.SessionId, new AgentMessage
        {
            Role = "user",
            Content = context.Message,
            Timestamp = context.StartedAt,
        }, ct);

        // Save assistant response
        if (context.ResponseText is not null)
        {
            await sessionStore.SaveMessageAsync(context.SessionId, new AgentMessage
            {
                Role = "assistant",
                Content = context.ResponseText,
                Timestamp = DateTimeOffset.UtcNow,
            }, ct);
        }
    }
}
```

- [ ] **Step 4: Update EfAgentSessionStore namespace**

Update `modules/Agents/src/SimpleModule.Agents.Module/EfAgentSessionStore.cs` to use `SimpleModule.Agents.Module.Sessions` namespace instead of `SimpleModule.Agents.Sessions`.

- [ ] **Step 5: Commit**

```bash
git add modules/Agents/src/SimpleModule.Agents.Module/Sessions/ \
       modules/Agents/src/SimpleModule.Agents.Module/Hooks/SessionHistoryHook.cs \
       modules/Agents/src/SimpleModule.Agents.Module/Options/SessionOptions.cs \
       modules/Agents/src/SimpleModule.Agents.Module/EfAgentSessionStore.cs
git commit -m "feat: add SessionHistoryHook with session store moved into Agents module"
```

### Task 8: Add RateLimitingHook and TelemetryHook to Agents module

**Files:**
- Create: `modules/Agents/src/SimpleModule.Agents.Module/Hooks/RateLimitingHook.cs`
- Create: `modules/Agents/src/SimpleModule.Agents.Module/Hooks/TelemetryHook.cs`
- Create: `modules/Agents/src/SimpleModule.Agents.Module/Options/RateLimitingOptions.cs`
- Create: `modules/Agents/src/SimpleModule.Agents.Module/Options/TelemetryOptions.cs`

- [ ] **Step 1: Create RateLimitingOptions and RateLimitingHook**

Port logic from the deleted `RateLimitingMiddleware.cs`. Key difference: it's now a hook that sets `IsRejected` instead of throwing.

```csharp
// modules/Agents/src/SimpleModule.Agents.Module/Options/RateLimitingOptions.cs
namespace SimpleModule.Agents.Module.Options;

public sealed class RateLimitingOptions
{
    public bool Enabled { get; set; }
    public int RequestsPerMinute { get; set; } = 60;
}
```

```csharp
// modules/Agents/src/SimpleModule.Agents.Module/Hooks/RateLimitingHook.cs
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using SimpleModule.Agents.Module.Options;
using SimpleModule.Core.Agents;

namespace SimpleModule.Agents.Module.Hooks;

public sealed class RateLimitingHook(IOptions<RateLimitingOptions> options) : AgentChatHookBase
{
    private readonly ConcurrentDictionary<string, List<DateTimeOffset>> _requests = new();
    public override int Order => 20;

    public override Task OnBeforeChatAsync(AgentChatContext context, CancellationToken ct)
    {
        var userId = context.User?.Identity?.Name ?? "anonymous";
        var key = $"{userId}:{context.AgentName}";
        var now = DateTimeOffset.UtcNow;
        var windowStart = now.AddSeconds(-60);

        var entries = _requests.GetOrAdd(key, _ => []);
        lock (entries)
        {
            entries.RemoveAll(t => t < windowStart);
            if (entries.Count >= options.Value.RequestsPerMinute)
            {
                context.IsRejected = true;
                context.RejectionReason = "Rate limit exceeded. Please try again later.";
                return Task.CompletedTask;
            }
            entries.Add(now);
        }

        return Task.CompletedTask;
    }
}
```

- [ ] **Step 2: Create TelemetryOptions and TelemetryHook**

```csharp
// modules/Agents/src/SimpleModule.Agents.Module/Options/TelemetryOptions.cs
namespace SimpleModule.Agents.Module.Options;

public sealed class TelemetryOptions
{
    public bool Enabled { get; set; }
}
```

```csharp
// modules/Agents/src/SimpleModule.Agents.Module/Hooks/TelemetryHook.cs
using System.Diagnostics;
using Microsoft.Extensions.Options;
using SimpleModule.Agents.Module.Options;
using SimpleModule.Core.Agents;

namespace SimpleModule.Agents.Module.Hooks;

public sealed class TelemetryHook(IOptions<TelemetryOptions> options) : AgentChatHookBase
{
    private static readonly ActivitySource Source = new("SimpleModule.Agents");
    public override int Order => 200;

    public override Task OnBeforeChatAsync(AgentChatContext context, CancellationToken ct)
    {
        var activity = Source.StartActivity("agent.invoke");
        if (activity is not null)
        {
            activity.SetTag("agent.name", context.AgentName);
            activity.SetTag("agent.user", context.User?.Identity?.Name);
            context.Properties["Activity"] = activity;
        }
        return Task.CompletedTask;
    }

    public override Task OnAfterChatAsync(AgentChatContext context, CancellationToken ct)
    {
        if (context.Properties.TryGetValue("Activity", out var obj) && obj is Activity activity)
        {
            try
            {
                activity.SetTag("agent.response_length", context.ResponseText?.Length ?? 0);
                activity.SetTag("agent.estimated_tokens", (context.ResponseText?.Length ?? 0) / 4);
            }
            finally
            {
                activity.Dispose(); // ensures span is always closed per spec
            }
        }
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add modules/Agents/src/SimpleModule.Agents.Module/Hooks/ \
       modules/Agents/src/SimpleModule.Agents.Module/Options/
git commit -m "feat: add RateLimitingHook and TelemetryHook to Agents module"
```

### Task 9: Rewrite AgentsModule with feature-flag registration

**Files:**
- Modify: `modules/Agents/src/SimpleModule.Agents.Module/AgentsModule.cs`
- Create: `modules/Agents/src/SimpleModule.Agents.Module/Options/AgentModuleOptions.cs`

- [ ] **Step 1: Create AgentModuleOptions**

```csharp
// modules/Agents/src/SimpleModule.Agents.Module/Options/AgentModuleOptions.cs
namespace SimpleModule.Agents.Module.Options;

public sealed class AgentModuleOptions
{
    public SessionOptions Sessions { get; set; } = new();
    public GuardrailOptions Guardrails { get; set; } = new();
    public RateLimitingOptions RateLimiting { get; set; } = new();
    public TelemetryOptions Telemetry { get; set; } = new();
}
```

- [ ] **Step 2: Rewrite AgentsModule**

```csharp
// modules/Agents/src/SimpleModule.Agents.Module/AgentsModule.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Agents.Module.Hooks;
using SimpleModule.Agents.Module.Options;
using SimpleModule.Agents.Module.Sessions;
using SimpleModule.Core;
using SimpleModule.Core.Agents;
using SimpleModule.Database;

namespace SimpleModule.Agents.Module;

[Module(AgentsConstants.ModuleName)]
public class AgentsModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("Agents").Get<AgentModuleOptions>() ?? new();

        services.AddModuleDbContext<AgentsDbContext>(configuration, AgentsConstants.ModuleName);

        // EF-backed session store (InMemoryAgentSessionStore is available as fallback
        // but only registered if no EF context — for this module, always use EF)
        services.AddScoped<IAgentSessionStore, EfAgentSessionStore>();

        // Feature-flagged hooks
        if (options.Sessions.Enabled)
        {
            services.Configure<SessionOptions>(configuration.GetSection("Agents:Sessions"));
            services.AddScoped<IAgentChatHook, SessionHistoryHook>();
        }

        if (options.Guardrails.Enabled)
        {
            services.Configure<GuardrailOptions>(configuration.GetSection("Agents:Guardrails"));
            services.AddSingleton<IAgentChatHook, GuardrailHook>();
        }

        if (options.RateLimiting.Enabled)
        {
            services.Configure<RateLimitingOptions>(configuration.GetSection("Agents:RateLimiting"));
            services.AddSingleton<IAgentChatHook, RateLimitingHook>();
        }

        if (options.Telemetry.Enabled)
        {
            services.Configure<TelemetryOptions>(configuration.GetSection("Agents:Telemetry"));
            services.AddSingleton<IAgentChatHook, TelemetryHook>();
        }
    }
}
```

- [ ] **Step 3: Update Agents module csproj — remove framework Agents dependency on Sessions namespace**

The Agents module csproj already references `SimpleModule.Agents` — it now needs to reference `SimpleModule.Core` for `IAgentChatHook`. Check the csproj still compiles. The `SimpleModule.Agents` reference is still needed for `IAgentRegistry` etc.

- [ ] **Step 4: Build Agents module**

Run: `dotnet build modules/Agents/src/SimpleModule.Agents.Module/SimpleModule.Agents.Module.csproj`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add modules/Agents/src/SimpleModule.Agents.Module/
git commit -m "refactor: rewrite AgentsModule with feature-flag hook registration"
```

---

## Chunk 5: Refactor Rag Module — Absorb Framework Packages

### Task 10: Move RAG framework code into Rag module

**Files:**
- Move contents of `framework/SimpleModule.Rag/` → `modules/Rag/src/SimpleModule.Rag.Module/`
- Move contents of `framework/SimpleModule.Rag.StructuredRag/` → `modules/Rag/src/SimpleModule.Rag.Module/StructuredRag/`
- Move contents of `framework/SimpleModule.Rag.VectorStore.InMemory/` → `modules/Rag/src/SimpleModule.Rag.Module/VectorStores/`
- Move contents of `framework/SimpleModule.Rag.VectorStore.Postgres/` → `modules/Rag/src/SimpleModule.Rag.Module/VectorStores/`
- Create: `modules/Rag/src/SimpleModule.Rag.Module/Hooks/RagContextHook.cs`

This is the largest task. The key steps:

- [ ] **Step 1: Copy RAG framework files into the module**

```bash
# Core RAG files
cp framework/SimpleModule.Rag/IRagPipeline.cs modules/Rag/src/SimpleModule.Rag.Module/
cp framework/SimpleModule.Rag/IKnowledgeStore.cs modules/Rag/src/SimpleModule.Rag.Module/
cp framework/SimpleModule.Rag/VectorKnowledgeStore.cs modules/Rag/src/SimpleModule.Rag.Module/
cp framework/SimpleModule.Rag/KnowledgeRecord.cs modules/Rag/src/SimpleModule.Rag.Module/
cp framework/SimpleModule.Rag/KnowledgeSearchResult.cs modules/Rag/src/SimpleModule.Rag.Module/
cp framework/SimpleModule.Rag/RagOptions.cs modules/Rag/src/SimpleModule.Rag.Module/
cp framework/SimpleModule.Rag/RagQueryOptions.cs modules/Rag/src/SimpleModule.Rag.Module/
cp framework/SimpleModule.Rag/RagResult.cs modules/Rag/src/SimpleModule.Rag.Module/
cp framework/SimpleModule.Rag/KnowledgeIndexingHostedService.cs modules/Rag/src/SimpleModule.Rag.Module/
cp framework/SimpleModule.Rag/RagSettingsDefinitions.cs modules/Rag/src/SimpleModule.Rag.Module/

# StructuredRag
mkdir -p modules/Rag/src/SimpleModule.Rag.Module/StructuredRag/Data
mkdir -p modules/Rag/src/SimpleModule.Rag.Module/StructuredRag/Preprocessing
cp framework/SimpleModule.Rag.StructuredRag/*.cs modules/Rag/src/SimpleModule.Rag.Module/StructuredRag/
cp framework/SimpleModule.Rag.StructuredRag/Data/*.cs modules/Rag/src/SimpleModule.Rag.Module/StructuredRag/Data/
cp framework/SimpleModule.Rag.StructuredRag/Preprocessing/*.cs modules/Rag/src/SimpleModule.Rag.Module/StructuredRag/Preprocessing/

# Vector stores
mkdir -p modules/Rag/src/SimpleModule.Rag.Module/VectorStores
cp framework/SimpleModule.Rag.VectorStore.InMemory/InMemoryVectorStoreExtensions.cs modules/Rag/src/SimpleModule.Rag.Module/VectorStores/
cp framework/SimpleModule.Rag.VectorStore.Postgres/PostgresVectorStoreExtensions.cs modules/Rag/src/SimpleModule.Rag.Module/VectorStores/
cp framework/SimpleModule.Rag.VectorStore.Postgres/PostgresVectorStoreOptions.cs modules/Rag/src/SimpleModule.Rag.Module/VectorStores/
```

- [ ] **Step 2: Update all namespaces in moved files**

All files moved into the module need their namespaces updated to `SimpleModule.Rag.Module` (or sub-namespaces). This is a bulk find-and-replace:
- `namespace SimpleModule.Rag;` → `namespace SimpleModule.Rag.Module;`
- `namespace SimpleModule.Rag.StructuredRag` → `namespace SimpleModule.Rag.Module.StructuredRag`
- `namespace SimpleModule.Rag.VectorStore.InMemory;` → `namespace SimpleModule.Rag.Module.VectorStores;`
- `namespace SimpleModule.Rag.VectorStore.Postgres;` → `namespace SimpleModule.Rag.Module.VectorStores;`
- Update `using` statements in each file accordingly.

- [ ] **Step 3: Create RagContextHook**

```csharp
// modules/Rag/src/SimpleModule.Rag.Module/Hooks/RagContextHook.cs
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using SimpleModule.Core.Agents;

namespace SimpleModule.Rag.Module.Hooks;

public sealed class RagContextHook(
    IRagPipeline ragPipeline,
    IOptions<RagOptions> options
) : AgentChatHookBase
{
    public override int Order => 110;

    public override async Task OnBeforeChatAsync(AgentChatContext context, CancellationToken ct)
    {
        if (context.AgentDefinition.EnableRag != true) return;

        var result = await ragPipeline.QueryAsync(context.Message, cancellationToken: ct);
        if (result.Sources.Count > 0)
        {
            var contextText = string.Join("\n\n",
                result.Sources.Select(s => $"### {s.Title}\n{s.Content}"));
            var ragMessage = new ChatMessage(ChatRole.System,
                $"## Retrieved Knowledge\n\n{contextText}");
            // Insert after system prompt, before any history or user message
            context.Messages.Insert(1, ragMessage);
        }
    }
}
```

- [ ] **Step 4: Update Rag module csproj with all needed dependencies**

Replace `modules/Rag/src/SimpleModule.Rag.Module/SimpleModule.Rag.Module.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Library</OutputType>
    <Description>RAG module for SimpleModule. Knowledge store, vector search, StructuredRag, and agent integration via RagContextHook.</Description>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="Microsoft.Extensions.AI.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.VectorData.Abstractions" />
    <PackageReference Include="Microsoft.SemanticKernel.Connectors.InMemory" />
    <PackageReference Include="Microsoft.SemanticKernel.Connectors.Postgres" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Core\SimpleModule.Core.csproj" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Database\SimpleModule.Database.csproj" />
  </ItemGroup>
</Project>
```

Note: No longer references `SimpleModule.Rag` or `SimpleModule.Rag.StructuredRag` framework packages — it IS the RAG code now.

- [ ] **Step 5: Rewrite RagModule with feature-flag registration**

```csharp
// modules/Rag/src/SimpleModule.Rag.Module/RagModule.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Core;
using SimpleModule.Core.Agents;
using SimpleModule.Database;
using SimpleModule.Rag.Module.Hooks;
using SimpleModule.Rag.Module.StructuredRag;
using SimpleModule.Rag.Module.StructuredRag.Data;
using SimpleModule.Rag.Module.VectorStores;

namespace SimpleModule.Rag.Module;

[Module(RagConstants.ModuleName)]
public class RagModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var ragSection = configuration.GetSection("Rag");
        services.Configure<RagOptions>(ragSection);
        services.AddModuleDbContext<RagDbContext>(configuration, RagConstants.ModuleName);

        // Knowledge store
        services.AddSingleton<IKnowledgeStore, VectorKnowledgeStore>();

        // Vector store backend
        var vectorStore = ragSection.GetValue<string>("VectorStore") ?? "InMemory";
        if (vectorStore.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
        {
            services.AddPostgresVectorStore(configuration);
        }
        else
        {
            services.AddInMemoryVectorStore();
        }

        // RAG pipeline
        var structuredRagEnabled = ragSection.GetValue<bool>("StructuredRag:Enabled");
        if (structuredRagEnabled)
        {
            services.Configure<StructuredRagOptions>(ragSection.GetSection("StructuredRag"));
            services.AddSingleton<IStructureRouter, LlmStructureRouter>();
            services.AddSingleton<IKnowledgeStructurizer, LlmKnowledgeStructurizer>();
            services.AddSingleton<IStructuredKnowledgeUtilizer, LlmStructuredKnowledgeUtilizer>();
            services.AddSingleton<IRagPipeline, StructuredRagPipeline>();
            services.AddScoped<IStructuredKnowledgeCache, EfStructuredKnowledgeCache>();
        }
        else
        {
            // SimpleRagPipeline — a basic pipeline that queries the knowledge store directly
            services.AddSingleton<IRagPipeline, SimpleRagPipeline>();
        }

        // Hook into agent chat
        services.AddScoped<IAgentChatHook, RagContextHook>();

        // Knowledge indexing at startup
        services.AddHostedService<KnowledgeIndexingHostedService>();
    }
}
```

Note: This references `SimpleRagPipeline` which needs to be created — a basic RAG that just queries the knowledge store without the 4-stage StructuredRag pipeline.

- [ ] **Step 6: Create SimpleRagPipeline**

```csharp
// modules/Rag/src/SimpleModule.Rag.Module/SimpleRagPipeline.cs
using Microsoft.Extensions.Options;

namespace SimpleModule.Rag.Module;

public sealed class SimpleRagPipeline(
    IKnowledgeStore knowledgeStore,
    IOptions<RagOptions> options
) : IRagPipeline
{
    public async Task<RagResult> QueryAsync(
        string query,
        RagQueryOptions? queryOptions = null,
        CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        var collectionName = queryOptions?.CollectionName ?? "default";
        var topK = queryOptions?.TopK ?? opts.DefaultTopK;
        var minScore = queryOptions?.MinScore ?? opts.MinScore;

        var results = await knowledgeStore.SearchAsync(
            collectionName, query, topK, minScore, cancellationToken);

        var sources = results.Select(r => new RagSource(
            r.Title, r.Content, r.Score, r.Metadata)).ToList();

        return new RagResult(
            Answer: "",
            Sources: sources,
            Metadata: new RagMetadata("SimpleRag", null, TimeSpan.Zero));
    }
}
```

- [ ] **Step 7: Build Rag module**

Run: `dotnet build modules/Rag/src/SimpleModule.Rag.Module/SimpleModule.Rag.Module.csproj`
Expected: Build succeeded (may need namespace fixes — iterate until green)

- [ ] **Step 8: Commit**

```bash
git add modules/Rag/src/SimpleModule.Rag.Module/
git commit -m "refactor: absorb RAG framework packages into Rag module with RagContextHook"
```

---

## Chunk 6: Delete Old Framework Packages & Update References

### Task 11: Delete old AI provider packages

**Files:**
- Delete: `framework/SimpleModule.AI.OpenAI/` (entire directory)
- Delete: `framework/SimpleModule.AI.Anthropic/` (entire directory)
- Delete: `framework/SimpleModule.AI.AzureOpenAI/` (entire directory)
- Delete: `framework/SimpleModule.AI.Ollama/` (entire directory)

- [ ] **Step 1: Delete directories**

```bash
rm -rf framework/SimpleModule.AI.OpenAI/
rm -rf framework/SimpleModule.AI.Anthropic/
rm -rf framework/SimpleModule.AI.AzureOpenAI/
rm -rf framework/SimpleModule.AI.Ollama/
```

- [ ] **Step 2: Remove from solution file**

Remove these lines from `SimpleModule.slnx`:
```xml
<Project Path="framework/SimpleModule.AI.Ollama/SimpleModule.AI.Ollama.csproj" />
<Project Path="framework/SimpleModule.AI.AzureOpenAI/SimpleModule.AI.AzureOpenAI.csproj" />
<Project Path="framework/SimpleModule.AI.OpenAI/SimpleModule.AI.OpenAI.csproj" />
<Project Path="framework/SimpleModule.AI.Anthropic/SimpleModule.AI.Anthropic.csproj" />
```

Add new entry:
```xml
<Project Path="framework/SimpleModule.AI/SimpleModule.AI.csproj" />
```

- [ ] **Step 3: Commit**

```bash
git add -A framework/SimpleModule.AI.OpenAI/ framework/SimpleModule.AI.Anthropic/ \
       framework/SimpleModule.AI.AzureOpenAI/ framework/SimpleModule.AI.Ollama/ \
       SimpleModule.slnx
git commit -m "refactor: delete old AI provider packages, replaced by SimpleModule.AI"
```

### Task 12: Delete old RAG framework packages

**Files:**
- Delete: `framework/SimpleModule.Rag/` (entire directory)
- Delete: `framework/SimpleModule.Rag.StructuredRag/` (entire directory)
- Delete: `framework/SimpleModule.Rag.VectorStore.InMemory/` (entire directory)
- Delete: `framework/SimpleModule.Rag.VectorStore.Postgres/` (entire directory)

- [ ] **Step 1: Delete directories**

```bash
rm -rf framework/SimpleModule.Rag/
rm -rf framework/SimpleModule.Rag.StructuredRag/
rm -rf framework/SimpleModule.Rag.VectorStore.InMemory/
rm -rf framework/SimpleModule.Rag.VectorStore.Postgres/
```

- [ ] **Step 2: Remove from solution file**

Remove these lines from `SimpleModule.slnx`:
```xml
<Project Path="framework/SimpleModule.Rag/SimpleModule.Rag.csproj" />
<Project Path="framework/SimpleModule.Rag.StructuredRag/SimpleModule.Rag.StructuredRag.csproj" />
<Project Path="framework/SimpleModule.Rag.VectorStore.InMemory/SimpleModule.Rag.VectorStore.InMemory.csproj" />
<Project Path="framework/SimpleModule.Rag.VectorStore.Postgres/SimpleModule.Rag.VectorStore.Postgres.csproj" />
```

- [ ] **Step 3: Commit**

```bash
git add -A framework/SimpleModule.Rag/ framework/SimpleModule.Rag.StructuredRag/ \
       framework/SimpleModule.Rag.VectorStore.InMemory/ framework/SimpleModule.Rag.VectorStore.Postgres/ \
       SimpleModule.slnx
git commit -m "refactor: delete old RAG framework packages, absorbed into Rag module"
```

### Task 13: Update Host project references

**Files:**
- Modify: `template/SimpleModule.Host/SimpleModule.Host.csproj`
- Modify: `template/SimpleModule.Host/Program.cs` (update registration calls)

- [ ] **Step 1: Update Host csproj**

Replace the old AI/Agent/Rag references with:
```xml
<ProjectReference Include="..\..\framework\SimpleModule.AI\SimpleModule.AI.csproj" />
<ProjectReference Include="..\..\framework\SimpleModule.Agents\SimpleModule.Agents.csproj" />
<ProjectReference Include="..\..\modules\Agents\src\SimpleModule.Agents.Module\SimpleModule.Agents.Module.csproj" />
<ProjectReference Include="..\..\modules\Rag\src\SimpleModule.Rag.Module\SimpleModule.Rag.Module.csproj" />
```

Remove references to:
- `SimpleModule.AI.Ollama`
- `SimpleModule.Rag`
- `SimpleModule.Rag.StructuredRag`
- `SimpleModule.Rag.VectorStore.InMemory`

- [ ] **Step 2: Update Program.cs registration calls**

Find and update the AI/Agent/Rag registration in `Program.cs`. Replace old calls like:
```csharp
services.AddOllamaAI(configuration);
services.AddSimpleModuleAgents(configuration);
services.AddSimpleModuleRag(configuration);
services.AddStructuredRag(configuration);
services.AddInMemoryVectorStore();
```

With:
```csharp
services.AddSimpleModuleAI(configuration);
services.AddSimpleModuleAgents(configuration);
```

The Rag and Agents modules register their own services via `ConfigureServices` (auto-discovered by the source generator).

- [ ] **Step 3: Build entire solution**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add template/SimpleModule.Host/SimpleModule.Host.csproj \
       template/SimpleModule.Host/Program.cs
git commit -m "refactor: update Host to use SimpleModule.AI and simplified agent registration"
```

---

## Chunk 7: Update Source Generator & Remove Core Dead Code

### Task 14: Update source generator — remove ConfigureAgents fully

**Files:**
- Modify: `framework/SimpleModule.Generator/Emitters/AgentExtensionsEmitter.cs`
- Modify: `framework/SimpleModule.Generator/Discovery/DiscoveryData.cs` (remove `HasConfigureAgents` from `ModuleInfoRecord` and `ModuleInfo`)
- Modify: `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs` (remove `HasConfigureAgents` detection logic)

- [ ] **Step 1: Remove ConfigureAgents from AgentExtensionsEmitter**

In `AgentExtensionsEmitter.cs`:

1. Remove `&& !data.Modules.Any(m => m.HasConfigureAgents)` from the early-return check (line 17)
2. Remove the entire `ConfigureAgents` section (lines 121-146):
   - The `var modulesWithAgents = ...` block
   - The `agentBuilder` generation
   - The `foreach` loops for manual registration

- [ ] **Step 2: Remove HasConfigureAgents from DiscoveryData**

In `framework/SimpleModule.Generator/Discovery/DiscoveryData.cs`:
- Remove the `HasConfigureAgents` property from `ModuleInfoRecord` and `ModuleInfo` types
- Remove any constructor parameter or assignment related to `HasConfigureAgents`

- [ ] **Step 3: Remove HasConfigureAgents detection from SymbolDiscovery**

In `framework/SimpleModule.Generator/Discovery/SymbolDiscovery.cs`:
- Find the code that sets `HasConfigureAgents` (detects if a module type overrides `ConfigureAgents`)
- Remove it entirely — the field no longer exists

- [ ] **Step 4: Update generator tests**

Check `tests/SimpleModule.Generator.Tests/` for any tests that exercise `ConfigureAgents` scenarios. Remove or update them to reflect that `ConfigureAgents` is no longer supported.

- [ ] **Step 5: Build generator**

Run: `dotnet build framework/SimpleModule.Generator/SimpleModule.Generator.csproj`
Expected: Build succeeded

- [ ] **Step 6: Run generator tests**

Run: `dotnet test tests/SimpleModule.Generator.Tests/`
Expected: All pass

- [ ] **Step 7: Commit**

```bash
git add framework/SimpleModule.Generator/ tests/SimpleModule.Generator.Tests/
git commit -m "refactor: remove ConfigureAgents support from source generator completely"
```

### Task 15: Update test infrastructure

**Files:**
- Modify: `tests/SimpleModule.Tests.Shared/Agents/AgentTestFixture.cs`
- Modify: `tests/SimpleModule.Tests.Shared/Agents/MockChatClient.cs`

- [ ] **Step 1: Update AgentTestFixture for new AgentChatService constructor**

`AgentChatService` now takes `IEnumerable<IAgentChatHook>` and `ILogger<AgentChatService>` as constructor parameters (in addition to the existing ones). Update the test fixture to:
- Pass an empty list of hooks `Enumerable.Empty<IAgentChatHook>()`
- Pass a `NullLogger<AgentChatService>` or mock logger
- Remove any references to deleted types (`IAgentSessionStore`, middleware, guardrails)
- Update the service to be resolved as `IAgentChatService` interface

- [ ] **Step 2: Verify test compilation**

Run: `dotnet build tests/SimpleModule.Tests.Shared/SimpleModule.Tests.Shared.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add tests/SimpleModule.Tests.Shared/
git commit -m "fix: update AgentTestFixture for hook-based AgentChatService"
```

### Task 16: Clean up Core dead code

**Files:**
- Delete: `framework/SimpleModule.Core/Agents/IAgentBuilder.cs` (if not already deleted in Task 2)
- Verify: `framework/SimpleModule.Core/Rag/` — interfaces stay (IKnowledgeSource, KnowledgeDocument, IKnowledgePreprocessor, StructureType)

- [ ] **Step 1: Verify Core Rag interfaces are unchanged**

These stay in Core so other modules can implement `IKnowledgeSource`:
- `framework/SimpleModule.Core/Rag/IKnowledgeSource.cs`
- `framework/SimpleModule.Core/Rag/KnowledgeDocument.cs`
- `framework/SimpleModule.Core/Rag/IKnowledgePreprocessor.cs`
- `framework/SimpleModule.Core/Rag/StructureType.cs`

No changes needed.

- [ ] **Step 2: Full solution build**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 3: Run all tests**

Run: `dotnet test`
Expected: All tests pass (some test files may need updates for namespace changes)

- [ ] **Step 4: Fix any compilation or test failures**

Iterate until green. Common issues:
- Namespace changes in test files referencing old `SimpleModule.Agents.Sessions`
- Missing `using` statements for `SimpleModule.Core.Agents` in files that previously imported middleware/guardrail namespaces
- `AgentTestFixture` and `MockChatClient` in test shared project may need updates

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "chore: fix compilation and test failures from architecture redesign"
```

---

## Chunk 8: Verification & Final Cleanup

### Task 17: Verify the architecture

- [ ] **Step 1: Verify package count**

```bash
find framework -name "*.csproj" | grep -i -E "(agent|ai|rag)" | wc -l
```
Expected: 2 (SimpleModule.AI, SimpleModule.Agents)

- [ ] **Step 2: Verify no dead code references**

```bash
grep -r "IAgentMiddleware\|IAgentGuardrail\|AgentFileService\|AgentBuilder\b" --include="*.cs" framework/
```
Expected: No matches

- [ ] **Step 3: Verify hooks are registered**

```bash
grep -r "IAgentChatHook" --include="*.cs" modules/
```
Expected: Matches in GuardrailHook, SessionHistoryHook, RateLimitingHook, TelemetryHook, RagContextHook, and AgentsModule/RagModule registration

- [ ] **Step 4: Verify all tests pass**

Run: `dotnet test`
Expected: All pass

- [ ] **Step 5: Verify build**

Run: `dotnet build`
Expected: 0 warnings related to agent/AI/RAG code

- [ ] **Step 6: Final commit if any cleanup was needed**

```bash
git add -A
git commit -m "chore: final cleanup after AI/agents architecture redesign"
```

### Task 18: Update example agent (Products module)

**Files:**
- Verify: `modules/Products/src/SimpleModule.Products/Agents/ProductSearchAgent.cs`
- Verify: `modules/Products/src/SimpleModule.Products/Agents/ProductToolProvider.cs`
- Verify: `modules/Products/src/SimpleModule.Products/Agents/ProductKnowledgeSource.cs`

- [ ] **Step 1: Verify example agents still compile**

These files should need no changes — they implement `IAgentDefinition`, `IAgentToolProvider`, and `IKnowledgeSource` which are unchanged in Core.

Run: `dotnet build modules/Products/src/SimpleModule.Products/SimpleModule.Products.csproj`
Expected: Build succeeded

- [ ] **Step 2: If any namespace changes needed, fix and commit**

```bash
git add -A modules/Products/
git commit -m "fix: update Products agent references for new architecture"
```
