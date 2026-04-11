using SimpleModule.Core.Events;

namespace SimpleModule.Users.Contracts.Events;

public sealed record UserRolesChangedEvent(UserId UserId, IReadOnlyList<string> Roles) : IEvent;
