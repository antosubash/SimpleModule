using SimpleModule.Core.Events;

namespace SimpleModule.Users.Contracts.Events;

public sealed record UserCreatedEvent(UserId UserId, string Email, string DisplayName) : IEvent;
