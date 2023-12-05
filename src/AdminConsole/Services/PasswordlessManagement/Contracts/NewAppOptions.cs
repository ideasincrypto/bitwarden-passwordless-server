namespace Passwordless.AdminConsole.Services.PasswordlessManagement.Contracts;

public class NewAppOptions
{
    public string AdminEmail { get; set; } = "";
    public bool EventLoggingIsEnabled { get; set; } = false;
    public int EventLoggingRetentionPeriod { get; set; } = 365;

    /// <summary>
    /// Maximum number of users allowed for this application.
    /// </summary>
    public long? MaxUsers { get; set; }
}