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
      "BaseUrl": "http://localhost:11434",
      "Model": "llama3"
    }
  }
}
```

## Registering the Agent Runtime

```csharp
builder.Services.AddSimpleModuleAgents(builder.Configuration);
```

This registers:
- `AgentChatService` -- handles chat requests (streaming and non-streaming)
- `IAgentRegistry` -- discovers and serves agent definitions
- `IAgentSessionStore` -- persists conversation history
- Middleware pipeline: logging, rate limiting, token tracking, retry
- Guardrails: content length limits, PII redaction, prompt injection detection

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
    public double? Temperature => 0.5;
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

## Agent Settings

The module registers these settings (manageable via the admin UI):

| Setting | Default | Description |
|---------|---------|-------------|
| `Agents.Enabled` | `true` | Global kill switch |
| `Agents.MaxTokens` | `4096` | Default max tokens per response |
| `Agents.Temperature` | `0.7` | Default sampling temperature |
| `Agents.EnableRag` | `true` | Enable RAG context injection |
| `Agents.EnableStreaming` | `true` | Allow streaming responses |
| `Agents.RateLimit.RequestsPerMinute` | `60` | Rate limit per user |
| `Agents.RateLimit.TokensPerMinute` | `100000` | Token rate limit per user |

## Next Steps

- [File Storage](/guide/file-storage) -- storing files for RAG knowledge indexing
- [Settings](/guide/settings) -- runtime configuration for agent behavior
- [Modules](/guide/modules) -- structuring agent tools within modules
