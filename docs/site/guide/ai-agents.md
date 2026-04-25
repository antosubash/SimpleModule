---
outline: deep
---

# AI Agents

SimpleModule includes a framework for building AI-powered agents with tool calling, RAG (retrieval-augmented generation), and streaming responses. The agent system supports multiple AI providers and is configured through the standard settings system.

## Architecture

The agent stack has three layers:

1. **AI Providers** (`SimpleModule.AI.*`) -- `IChatClient` implementations for different LLM providers
2. **Agent Runtime** (`SimpleModule.Agents`) -- orchestration, tool discovery, session management, and middleware
3. **RAG Pipeline** (`SimpleModule.Rag`) -- knowledge indexing and retrieval for context injection

## Setting Up an AI Provider

Register one AI provider in your host application. Each provider reads from a dedicated configuration section.

### Anthropic (Claude)

```csharp
builder.Services.AddAnthropicAI(builder.Configuration);
```

```json
{
  "AI": {
    "Anthropic": {
      "ApiKey": "sk-ant-...",
      "Model": "claude-sonnet-4-20250514"
    }
  }
}
```

::: warning Model selection not yet wired
`AnthropicOptions.Model` is bound from configuration, but `AddAnthropicAI` currently constructs the client with only the API key and returns `client.Messages` as the `IChatClient`. The configured `Model` value is not passed to the client today — model selection happens at call time or is left to the SDK default. Track this if you rely on the configured model taking effect.
:::

### OpenAI

```csharp
builder.Services.AddOpenAI(builder.Configuration);
```

```json
{
  "AI": {
    "OpenAI": {
      "ApiKey": "sk-...",
      "Model": "gpt-4o"
    }
  }
}
```

### Azure OpenAI

```csharp
builder.Services.AddAzureOpenAI(builder.Configuration);
```

```json
{
  "AI": {
    "AzureOpenAI": {
      "Endpoint": "https://your-resource.openai.azure.com",
      "DeploymentName": "gpt-4o",
      "ApiKey": "your-key"
    }
  }
}
```

### Ollama (Local)

```csharp
builder.Services.AddOllamaAI(builder.Configuration);
```

```json
{
  "AI": {
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "Model": "llama3.2"
    }
  }
}
```

## Registering the Agent Runtime

```csharp
builder.Services.AddSimpleModuleAgents(builder.Configuration);
```

`AddSimpleModuleAgents` itself registers only:

- `AgentOptions` -- bound from the `Agents:` configuration section
- `AgentChatService` -- handles chat requests (streaming and non-streaming)

The rest of the stack is wired in separately:

- **`IAgentRegistry`** and concrete `IAgentDefinition` implementations are registered by the source generator (`AgentExtensionsEmitter`) when it discovers agents in referenced assemblies.
- **`IAgentSessionStore`** lives in the separate `SimpleModule.Agents.Module` package; add that module if you need persisted conversation history.
- **`IAgentMiddleware`** and **`IAgentGuardrail`** are contracts only. `AgentMiddlewarePipeline` runs whatever implementations you register in DI — no middleware or guardrails ship as defaults. Register your own (for logging, rate limiting, PII redaction, etc.) explicitly.

## Defining an Agent

Implement `IAgentDefinition` in your module:

```csharp
public class ProductAssistant : IAgentDefinition
{
    public string Name => "product-assistant";
    public string Description => "Helps users find and manage products";
    public string Instructions => """
        You are a product catalog assistant.
        Help users search, compare, and manage products.
        """;

    // Optional overrides
    public int? MaxTokens => 2048;
    public float? Temperature => 0.5f;
    public bool? EnableRag => true;
    public string? RagCollectionName => "products";
}
```

## Creating Agent Tools

Implement `IAgentToolProvider` and mark methods with `[AgentTool]`:

```csharp
public class ProductTools(IProductContracts products) : IAgentToolProvider
{
    [AgentTool(Name = "search_products", Description = "Search the product catalog")]
    public async Task<List<ProductDto>> SearchAsync(string query, CancellationToken ct)
    {
        return await products.SearchAsync(query, ct);
    }

    [AgentTool(Name = "get_product", Description = "Get product details by ID")]
    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await products.GetByIdAsync(id, ct);
    }
}
```

Tools are automatically discovered via DI and converted to AI function definitions that the LLM can call.

## Chat API

### Non-Streaming

```csharp
var response = await agentChatService.ChatAsync(
    "product-assistant",
    new AgentChatRequest { Message = "Find me all products under $50" },
    cancellationToken);
```

### Streaming (SSE)

```csharp
await foreach (var chunk in agentChatService.ChatStreamAsync(
    "product-assistant",
    new AgentChatRequest { Message = "Compare these two products" },
    cancellationToken))
{
    // Send chunk to client via SSE
}
```

## RAG Integration

When `EnableRag` is `true` on an agent definition, the runtime automatically:

1. Queries the knowledge base via `IRagPipeline.QueryAsync()`
2. Injects matching knowledge chunks into the system message
3. Sends the enriched context to the LLM

### Setting Up RAG

```csharp
builder.Services.AddSimpleModuleRag(builder.Configuration);
```

```json
{
  "Rag": {
    "DefaultTopK": 5,
    "MinScore": 0.7,
    "EmbeddingDimension": 1536,
    "IndexOnStartup": true
  }
}
```

Choose a vector store:

```csharp
// Development
builder.Services.AddInMemoryVectorStore();

// Production
builder.Services.AddPostgresVectorStore(builder.Configuration);
```

## Agent Configuration

`AddSimpleModuleAgents` binds `AgentOptions` from the `Agents:` configuration section. These are plain `IOptions<AgentOptions>` values — **not** admin-UI `SettingDefinition`s — so they are configured via `appsettings.json` (or any standard configuration source) and only take effect on app start / options reload.

| Key | Default | Description |
|-----|---------|-------------|
| `Agents:Enabled` | `true` | Global kill switch |
| `Agents:MaxTokens` | `4096` | Default max tokens per response |
| `Agents:Temperature` | `0.7` | Default sampling temperature (`float`) |
| `Agents:EnableRag` | `true` | Enable RAG context injection |
| `Agents:EnableStreaming` | `true` | Allow streaming responses |
| `Agents:SessionTimeout` | `00:30:00` | Idle timeout for agent sessions |
| `Agents:RateLimit:RequestsPerMinute` | `60` | Rate limit per user |
| `Agents:RateLimit:TokensPerMinute` | `100000` | Token rate limit per user |

Example:

```json
{
  "Agents": {
    "Enabled": true,
    "MaxTokens": 4096,
    "Temperature": 0.7,
    "EnableRag": true,
    "EnableStreaming": true,
    "SessionTimeout": "00:30:00",
    "RateLimit": {
      "RequestsPerMinute": 60,
      "TokensPerMinute": 100000
    }
  }
}
```

## Next Steps

- [File Storage](/guide/file-storage) -- storing files for RAG knowledge indexing
- [Settings](/guide/settings) -- runtime configuration for agent behavior
- [Modules](/guide/modules) -- structuring agent tools within modules
