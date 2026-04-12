using SimpleModule.Core.Events;

namespace SimpleModule.Users.Contracts.Events;

public sealed record UserDeletedEvent(UserId UserId) : IEvent;
