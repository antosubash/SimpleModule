using Vogen;

namespace SimpleModule.Chat.Contracts;

[ValueObject<Guid>(conversions: Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct ConversationId;
