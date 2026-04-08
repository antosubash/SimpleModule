using System.Text.Json.Serialization;

// Wire types for the @tanstack/ai SSE protocol. Field names are load-bearing —
// they must serialize to exactly the JSON keys the TanStack client expects.

namespace SimpleModule.Chat.Dtos;

public sealed record TanStackChatRequest(
    IReadOnlyList<TanStackInboundMessage> Messages,
    Dictionary<string, object?>? Data = null
);

public sealed record TanStackInboundMessage(string Role, string Content);

public sealed record TanStackContentChunk(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("timestamp")] long Timestamp,
    [property: JsonPropertyName("delta")] string Delta,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("role")] string Role
);

public sealed record TanStackDoneChunk(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("timestamp")] long Timestamp,
    [property: JsonPropertyName("finishReason")] string FinishReason
);

public sealed record TanStackErrorChunk(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("timestamp")] long Timestamp,
    [property: JsonPropertyName("error")] TanStackErrorBody Error
);

public sealed record TanStackErrorBody(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("code")] string Code
);

public sealed record UiMessage(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("parts")] IReadOnlyList<UiMessagePart> Parts,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt
);

public sealed record UiMessagePart(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("content")] string Content
);
