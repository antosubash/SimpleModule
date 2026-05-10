using SimpleModule.Core.Events;

namespace SimpleModule.Users.Contracts.Events;

public sealed record UserSelfUnlockedEvent(UserId UserId, string Email) : IEvent;
