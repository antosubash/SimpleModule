# SimpleModule.Chat

User-facing chat module for [SimpleModule](https://github.com/antosubash/SimpleModule) — a modular monolith framework for .NET.

Provides a conversations UI on top of the [Agents](../../../Agents/src/SimpleModule.Agents.Module) framework and its built-in RAG pipeline. The frontend uses [@tanstack/ai-react](https://github.com/tanstack/ai) for streaming chat rendering; the backend emits the TanStack SSE wire protocol.

## Features

- Persistent conversations per user with titles, pinning, timestamps
- Streaming replies via Server-Sent Events in the TanStack `StreamChunk` format
- Multi-turn history replay through `AgentChatService` (full context every turn)
- Automatic RAG augmentation — inherited from the agent definition's `EnableRag` / `RagCollectionName`
- Per-user isolation enforced at the service layer (other users' conversations return 404, not 403)
- Permission-gated endpoints: `Chat.View`, `Chat.Create`, `Chat.ManageAll`
- Graceful mid-stream error handling — LLM failures are delivered as TanStack `error` chunks; partial assistant replies are still persisted

## Routes

**API (`/api/chat`)**
- `GET  /conversations` — list the current user's conversations
- `POST /conversations` — start a new conversation (`{ agentName, title? }`)
- `GET  /conversations/{id}` — get a conversation with its messages
- `PATCH /conversations/{id}` — rename (`{ title }`)
- `DELETE /conversations/{id}` — delete (cascades messages)
- `GET  /conversations/{id}/messages` — full message history
- `POST /conversations/{id}/stream` — SSE stream; accepts the TanStack `{ messages, data? }` body

**Views (`/chat`)**
- `/chat` — conversation list + new-chat picker
- `/chat/{id}` — conversation view with streaming hook via `useChat`

## Wire protocol

The `/stream` endpoint emits SSE frames in the [TanStack AI protocol](https://github.com/tanstack/ai/blob/main/docs/protocol/sse-protocol.md):

```
data: {"type":"content","id":"msg-…","model":"agent-name","timestamp":…,"delta":"Hello","content":"Hello","role":"assistant"}
data: {"type":"content",…,"delta":" world","content":"Hello world","role":"assistant"}
data: {"type":"done","id":"msg-…","model":"agent-name","timestamp":…,"finishReason":"stop"}
data: [DONE]
```

On failure:

```
data: {"type":"content",…"delta":"Hel"…}
data: {"type":"error","id":"msg-…","error":{"message":"network blip","code":"agent_error"}}
data: [DONE]
```

The frontend consumes this via `fetchServerSentEvents('/api/chat/conversations/{id}/stream')` passed to `useChat`.

## Public API for other modules

```csharp
public interface IChatContracts
{
    Task<IReadOnlyList<Conversation>> GetUserConversationsAsync(string userId, CancellationToken ct = default);
    Task<Conversation?> GetConversationAsync(ConversationId id, CancellationToken ct = default);
    Task<Conversation> StartConversationAsync(string userId, string agentName, string? title, CancellationToken ct = default);
}
```

Inject `IChatContracts` to launch pre-seeded chats from elsewhere (e.g. a "Help" button on any page).

## Data model

- `ChatConversations` — one row per conversation (`Id`, `UserId`, `Title`, `AgentName`, `Pinned`, timestamps)
- `ChatMessages` — one row per turn (`Id`, `ConversationId`, `Role`, `Content`, `CreatedAt`)

Chat owns its own persistence rather than delegating to the Agents module's `AgentSession` store, because chat needs UI-scoped metadata (title, pinning) and a stable history for rendering even when the LLM is stateless per turn.

## Tests

74 tests covering unit (service, DTO serialization, value objects, history replay) and integration (CRUD endpoints over HTTP, streaming endpoint with a fake `IChatClient`, error paths, permission enforcement, multi-user isolation).

```bash
dotnet test modules/Chat/tests/SimpleModule.Chat.Tests
```

## License

MIT
