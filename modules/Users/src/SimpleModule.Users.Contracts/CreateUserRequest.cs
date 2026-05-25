namespace SimpleModule.Users.Contracts;

public class CreateUserRequest
{
    public string? Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
