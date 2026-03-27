namespace SimpleModule.Users.Contracts;

public class UpdateAdminUserRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
}
