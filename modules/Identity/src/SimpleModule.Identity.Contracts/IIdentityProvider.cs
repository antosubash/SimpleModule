namespace SimpleModule.Identity.Contracts;

public interface IIdentityProvider
{
    string Name { get; }
    bool SupportsLocalUsers { get; }
}
