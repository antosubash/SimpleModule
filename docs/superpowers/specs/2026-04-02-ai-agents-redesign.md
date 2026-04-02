# AI & Agents Architecture Redesign

## Problem

The current AI/agents framework has 10 packages, 3 competing extension mechanisms (middleware pipeline, guardrails, events), a god-class orchestrator (`AgentChatService`), and significant dead code (guardrails registered but never invoked, middleware pipeline implemented but never used, session store abstracted but unused in chat flow). The framework does too much — it needs to be a thin extension point where modules opt into features.

## Decision

Restructure into a **thin core + hooks** architecture:

- **2 framework packages** instead of 10
- **1 extension mechanism** (`IAgentChatHook`) replaces middleware, guardrails, and events
- **Features move into modules** with configuration-based feature flags
- All dead code paths are fixed: everything registered actually gets invoked

## Package Layout

### Framework (2 packages)

```
framework/SimpleModule.AI/              ← merged: OpenAI, Anthropic, Azure, Ollama
framework/SimpleModule.Agents/          ← slim: registry, chat service, hooks, endpoints
```

### Core Interfaces (in SimpleModule.Core)

```
Core/Agents/
├── IAgentDefinition                    (existing, unchanged)
├── IAgentToolProvider                  (existing, unchanged)
├── AgentToolAttribute                  (existing, unchanged)
├── IAgentChatService                   (new interface)
├── IAgentChatHook                      (new, the single extension point)
└── AgentChatContext                    (new, the context bag)
```

### Modules Own the Features

```
modules/Agents/     ← sessions, guardrails, rate limiting, telemetry, playground
modules/Rag/        ← knowledge store, vector search, indexing, StructuredRag, vector backends
```

## The Hook System

Replaces both the middleware pipeline and the guardrail system with a single interface.

### Interface

```csharp
public interface IAgentChatHook
{
    /// Lower Order runs first. Ranges:
    /// 0-99: security (guardrails, rate limiting)
    /// 100-199: context enrichment (RAG, session history)
    /// 200-299: observability (logging, telemetry, token tracking)
    int Order { get; }

    Task OnBeforeChatAsync(AgentChatContext context, CancellationToken ct);
    Task OnAfterChatAsync(AgentChatContext context, CancellationToken ct);
}
```

### Context

```csharp
public class AgentChatContext
{
    public string AgentName { get; }
    public AgentRegistration Agent { get; }
    public AgentChatRequest Request { get; }
    public ClaimsPrincipal? User { get; }
    public List<ChatMessage> Messages { get; }           // hooks can add/modify
    public ChatOptions ChatOptions { get; }               // hooks can modify tools, temperature
    public AgentChatResponse? Response { get; set; }      // set after LLM call
    public bool IsRejected { get; set; }                  // guardrails set this to block
    public string? RejectionReason { get; set; }
    public Dictionary<string, object> Properties { get; } // cross-hook state
    public DateTimeOffset StartedAt { get; }
}
```

### Execution Flow

```
ChatAsync(agentName, request):
  1. Build AgentChatContext with system message + user message + resolved tools
  2. Run all hooks.OnBeforeChatAsync() ordered by Order ascending
     - If context.IsRejected → return rejection response immediately
  3. Call IChatClient.GetResponseAsync() with context.Messages + context.ChatOptions
  4. Set context.Response
  5. Run all hooks.OnAfterChatAsync() in reverse Order (descending)
  6. Return response
```

Streaming follows the same pattern — hooks run before/after the stream, not per-chunk.

## Slim AgentChatService

The refactored orchestrator has one job: run hooks → call LLM.

```csharp
public class AgentChatService : IAgentChatService
{
    private readonly IAgentRegistry _registry;
    private readonly IChatClient _chatClient;
    private readonly IEnumerable<IAgentChatHook> _hooks;
    private readonly IServiceProvider _serviceProvider;

    public async Task<AgentChatResponse> ChatAsync(
        string agentName, AgentChatRequest request, ClaimsPrincipal? user, CancellationToken ct)
    {
        var agent = _registry.Get(agentName);
        var context = new AgentChatContext(agent, request, user);

        // System message from agent definition
        context.Messages.Add(new SystemChatMessage(agent.Instructions));
        context.Messages.Add(new UserChatMessage(request.Message));

        // Build tools from tool providers
        context.ChatOptions.Tools = ResolveTools(agent, _serviceProvider);

        // Before hooks (guardrails, RAG injection, session history load)
        foreach (var hook in _hooks.OrderBy(h => h.Order))
        {
            await hook.OnBeforeChatAsync(context, ct);
            if (context.IsRejected)
                return AgentChatResponse.Rejected(context.RejectionReason);
        }

        // LLM call
        var response = await _chatClient.GetResponseAsync(
            context.Messages, context.ChatOptions, ct);
        context.Response = MapResponse(response);

        // After hooks (session save, telemetry, token tracking)
        foreach (var hook in _hooks.OrderByDescending(h => h.Order))
            await hook.OnAfterChatAsync(context, ct);

        return context.Response;
    }
}
```

~40 lines. All cross-cutting concerns live in hooks.

## SimpleModule.AI — Merged Providers

Single package replaces 4 provider packages.

### Registration API

```csharp
// Config-driven (reads "AI:Provider")
services.AddSimpleModuleAI(config);

// Or explicit builder
services.AddSimpleModuleAI(ai => ai.UseOpenAI(config));
services.AddSimpleModuleAI(ai => ai.UseAnthropic(config));
services.AddSimpleModuleAI(ai => ai.UseAzureOpenAI(config));
services.AddSimpleModuleAI(ai => ai.UseOllama(config));
```

### Configuration

```json
{
  "AI": {
    "Provider": "OpenAI",
    "OpenAI": { "ApiKey": "...", "Model": "gpt-4o", "EmbeddingModel": "text-embedding-3-small" },
    "Anthropic": { "ApiKey": "...", "Model": "claude-sonnet-4-20250514" },
    "AzureOpenAI": { "Endpoint": "...", "ApiKey": "...", "DeploymentName": "..." },
    "Ollama": { "Endpoint": "http://localhost:11434", "Model": "llama3" }
  }
}
```

Registers `IChatClient` and `IEmbeddingGenerator<string, Embedding<float>>` in DI.

## Agents Module — Feature Flags

The Agents module registers hooks based on configuration. One module, features toggled by config.

### Registration

```csharp
[Module("agents", RoutePrefix = "agents")]
public class AgentsModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        var options = config.GetSection("Agents").Get<AgentModuleOptions>();

        services.AddDbContext<AgentsDbContext>();

        if (options?.Sessions?.Enabled != false)      // on by default
            services.AddScoped<IAgentChatHook, SessionHistoryHook>();

        if (options?.Guardrails?.Enabled != false)     // on by default
            services.AddSingleton<IAgentChatHook, GuardrailHook>();

        if (options?.RateLimiting?.Enabled == true)     // off by default
            services.AddSingleton<IAgentChatHook, RateLimitingHook>();

        if (options?.Telemetry?.Enabled == true)        // off by default
            services.AddSingleton<IAgentChatHook, TelemetryHook>();
    }
}
```

### Configuration

```json
{
  "Agents": {
    "Sessions": { "Enabled": true, "MaxHistory": 50 },
    "Guardrails": {
      "Enabled": true,
      "MaxInputLength": 10000,
      "MaxOutputLength": 50000,
      "PiiRedaction": true,
      "PromptInjectionDetection": true
    },
    "RateLimiting": { "Enabled": false, "RequestsPerMinute": 60 },
    "Telemetry": { "Enabled": false }
  }
}
```

### Hook Responsibilities

| Hook | Order | OnBeforeChat | OnAfterChat |
|------|-------|--------------|-------------|
| `GuardrailHook` | 10 | Validates input (length, PII, injection). Sets `IsRejected` if blocked. | Sanitizes output (PII redaction). |
| `RateLimitingHook` | 20 | Checks per-user rate. Rejects if exceeded. | — |
| `SessionHistoryHook` | 100 | Loads conversation history into `context.Messages`. | Saves user message + response to session store. |
| `TelemetryHook` | 200 | Starts `Activity` span. | Ends span, records token usage. |

`GuardrailHook` consolidates the 3 separate guardrail classes (ContentLength, PiiRedaction, PromptInjection) into one class with internal checks toggled by config flags.

## Rag Module — Plugs In Via Hook

The Rag module owns all RAG infrastructure and integrates with agents via `RagContextHook`.

### Registration

```csharp
[Module("rag", RoutePrefix = "rag")]
public class RagModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        var options = config.GetSection("Rag").Get<RagModuleOptions>();

        services.AddDbContext<RagDbContext>();

        // Knowledge store
        services.AddSingleton<IKnowledgeStore, VectorKnowledgeStore>();

        // Vector store backend
        if (options?.VectorStore == "Postgres")
            services.AddPostgresVectorStore(config);
        else
            services.AddInMemoryVectorStore();

        // RAG pipeline
        if (options?.StructuredRag?.Enabled == true)
            services.AddSingleton<IRagPipeline, StructuredRagPipeline>();
        else
            services.AddSingleton<IRagPipeline, SimpleRagPipeline>();

        // Hook into agent chat
        services.AddScoped<IAgentChatHook, RagContextHook>();

        // Knowledge indexing at startup
        services.AddHostedService<KnowledgeIndexingHostedService>();
    }
}
```

### RagContextHook

```csharp
public class RagContextHook : IAgentChatHook
{
    public int Order => 110;  // after session history, before telemetry

    public async Task OnBeforeChatAsync(AgentChatContext context, CancellationToken ct)
    {
        if (!context.Agent.EnableRag) return;

        var result = await _ragPipeline.QueryAsync(context.Request.Message, ct: ct);
        if (result.Sources.Count > 0)
        {
            var ragMessage = FormatSources(result.Sources);
            context.Messages.Insert(1, new SystemChatMessage(ragMessage));
        }
    }

    public Task OnAfterChatAsync(AgentChatContext context, CancellationToken ct)
        => Task.CompletedTask;
}
```

### What Moves Into This Module

Everything currently in these framework packages moves here:

- `SimpleModule.Rag` → `modules/Rag/src/SimpleModule.Rag.Module/`
- `SimpleModule.Rag.StructuredRag` → `modules/Rag/src/SimpleModule.Rag.Module/StructuredRag/`
- `SimpleModule.Rag.VectorStore.InMemory` → `modules/Rag/src/SimpleModule.Rag.Module/VectorStores/`
- `SimpleModule.Rag.VectorStore.Postgres` → `modules/Rag/src/SimpleModule.Rag.Module/VectorStores/`

The interfaces (`IKnowledgeSource`, `KnowledgeDocument`) stay in `SimpleModule.Core` so other modules can provide knowledge sources.

## Dead Code Removal

Removed entirely — not refactored:

| What | Why |
|------|-----|
| `IAgentMiddleware` + `AgentMiddlewarePipeline` + 4 middleware classes | Replaced by `IAgentChatHook` |
| `IAgentGuardrail` + `GuardrailResult` + `GuardrailDirection` + 3 guardrail classes | Consolidated into `GuardrailHook` |
| `AgentFileService` | Never had endpoints, incomplete feature |
| `AgentChatRequest.ResponseType` | Never used in any code path |
| `DevTools/AgentPlaygroundEndpoints` | Moves to Agents module |
| `IModule.ConfigureAgents()` | Source generator handles discovery |

## Source Generator Changes

`AgentExtensionsEmitter` continues to generate:

- `AddModuleAgents()` — discovers `IAgentDefinition`, `IAgentToolProvider`, `IKnowledgeSource`
- `MapModuleAgentEndpoints()` — maps agent REST endpoints

No changes needed to the generator logic itself. The generated code calls into the slimmed-down `SimpleModule.Agents` framework package.

## REST Endpoints (unchanged)

Stay in `framework/SimpleModule.Agents/AgentEndpoints.cs`:

- `GET /api/agents` — list registered agents
- `POST /api/agents/{name}/chat` — send message, get response
- `POST /api/agents/{name}/chat/stream` — SSE streaming

The structured output endpoint is removed (was never implemented).

## Migration Summary

| Before | After |
|--------|-------|
| 10 framework packages | 2 (`SimpleModule.AI`, `SimpleModule.Agents`) |
| 3 extension mechanisms (middleware, guardrails, events) | 1 (`IAgentChatHook`) |
| `AgentChatService` ~200 lines, 6 concerns | ~40 lines, hooks only |
| Guardrails: 3 classes, never invoked | 1 hook, actually runs |
| Sessions: abstracted but unused in chat | Hook, loads/saves history |
| RAG: framework concern (3 packages) | Module concern, plugs in via hook |
| Dead code: middleware pipeline, file service, response type | Removed |
| Configuration: implicit (everything always on) | Feature flags per module |
