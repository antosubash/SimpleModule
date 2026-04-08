using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Agents;
using SimpleModule.Agents.Dtos;
using SimpleModule.Chat.Contracts;
using SimpleModule.Chat.Dtos;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Exceptions;

namespace SimpleModule.Chat.Endpoints.Chat;

public class SendMessageStreamEndpoint : IEndpoint
{
    public const string Route = ChatConstants.Routes.SendMessageStream;

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                Route,
                async (
                    Guid id,
                    TanStackChatRequest request,
                    ChatService service,
                    AgentChatService agentChat,
                    ClaimsPrincipal user,
                    HttpContext httpContext,
                    CancellationToken ct
                ) =>
                {
                    if (request.Messages is null || request.Messages.Count == 0)
                    {
                        throw new ValidationException("messages is required.");
                    }

                    var lastUserIndex = -1;
                    for (var i = request.Messages.Count - 1; i >= 0; i--)
                    {
                        if (
                            string.Equals(
                                request.Messages[i].Role,
                                "user",
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        {
                            lastUserIndex = i;
                            break;
                        }
                    }
                    if (lastUserIndex < 0)
                    {
                        throw new ValidationException(
                            "messages must contain at least one user message."
                        );
                    }
                    var lastUser = request.Messages[lastUserIndex];
                    if (string.IsNullOrWhiteSpace(lastUser.Content))
                    {
                        throw new ValidationException("latest user message cannot be empty.");
                    }

                    var history = request
                        .Messages.Take(lastUserIndex)
                        .Where(m =>
                            !string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase)
                        )
                        .Select(m => new AgentHistoryMessage(m.Role, m.Content))
                        .ToArray();

                    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
                    var conversationId = ConversationId.From(id);
                    var conversation = await service.LoadOwnedAsync(conversationId, userId, ct);

                    // Persist user message first so history is correct even if streaming fails.
                    await service.AppendMessageAsync(
                        conversationId,
                        ChatRole.User,
                        lastUser.Content,
                        ct
                    );

                    httpContext.Response.ContentType = "text/event-stream";
                    httpContext.Response.Headers.CacheControl = "no-cache";
                    httpContext.Response.Headers.Connection = "keep-alive";

                    var messageId = $"msg-{Guid.NewGuid():N}";
                    var model = conversation.AgentName;
                    var assistantBuffer = new StringBuilder();

                    var agentRequest = new AgentChatRequest(
                        lastUser.Content,
                        SessionId: conversationId.Value.ToString(),
                        History: history
                    );

                    try
                    {
                        await foreach (
                            var chunk in agentChat.ChatStreamAsync(
                                conversation.AgentName,
                                agentRequest,
                                ct
                            )
                        )
                        {
                            assistantBuffer.Append(chunk);
                            var contentChunk = new TanStackContentChunk(
                                Type: "content",
                                Id: messageId,
                                Model: model,
                                Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                                Delta: chunk,
                                Content: chunk,
                                Role: "assistant"
                            );
                            await WriteSseAsync(httpContext, contentChunk, ct);
                        }

                        var doneChunk = new TanStackDoneChunk(
                            Type: "done",
                            Id: messageId,
                            Model: model,
                            Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            FinishReason: "stop"
                        );
                        await WriteSseAsync(httpContext, doneChunk, ct);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        // Client disconnected — do not emit an error chunk, just bail.
                    }
#pragma warning disable CA1031 // we emit the error back over SSE instead of 500
                    catch (Exception ex)
                    {
                        var errorChunk = new TanStackErrorChunk(
                            Type: "error",
                            Id: messageId,
                            Model: model,
                            Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            Error: new TanStackErrorBody(Message: ex.Message, Code: "agent_error")
                        );
                        await WriteSseAsync(httpContext, errorChunk, CancellationToken.None);
                    }
#pragma warning restore CA1031

                    if (assistantBuffer.Length > 0)
                    {
                        await service.AppendMessageAsync(
                            conversationId,
                            ChatRole.Assistant,
                            assistantBuffer.ToString(),
                            CancellationToken.None
                        );
                    }

                    await httpContext.Response.WriteAsync(
                        "data: [DONE]\n\n",
                        CancellationToken.None
                    );
                    await httpContext.Response.Body.FlushAsync(CancellationToken.None);
                }
            )
            .RequirePermission(ChatPermissions.Create);

    private static async Task WriteSseAsync<T>(
        HttpContext httpContext,
        T payload,
        CancellationToken ct
    )
    {
        var json = JsonSerializer.Serialize(payload);
        await httpContext.Response.WriteAsync($"data: {json}\n\n", ct);
        await httpContext.Response.Body.FlushAsync(ct);
    }
}
