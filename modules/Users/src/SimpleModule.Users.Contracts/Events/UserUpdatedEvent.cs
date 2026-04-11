using SimpleModule.Core.Events;

namespace SimpleModule.Users.Contracts.Events;

public sealed record UserUpdatedEvent(UserId UserId, string Email, string DisplayName) : IEvent;
