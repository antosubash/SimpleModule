namespace SimpleModule.Email;

public class EmailModuleOptions
{
    public string Provider { get; set; } = "Log";
    public SmtpOptions Smtp { get; set; } = new();
    public string DefaultFromAddress { get; set; } = "noreply@localhost";
    public string DefaultFromName { get; set; } = "SimpleModule";
    public int MaxRetryCount { get; set; } = 3;
    public string RetryIntervalCron { get; set; } = "*/5 * * * *";
}

public class SmtpOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool UseSsl { get; set; } = true;
}
