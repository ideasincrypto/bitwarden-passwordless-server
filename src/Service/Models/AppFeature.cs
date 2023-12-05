namespace Passwordless.Service.Models;

public class AppFeature : PerTenant
{
    public bool EventLoggingIsEnabled { get; set; }

    /// <summary>
    /// The event logging retention period in days.
    /// </summary>
    public int EventLoggingRetentionPeriod { get; set; }

    /// <summary>
    /// Developer logging is only enabled when an end date has been set, and has to be manually re-enabled every time.
    /// </summary>
    public DateTime? DeveloperLoggingEndsAt { get; set; }

    /// <summary>
    /// Maximum number of individual users allowed to use the application
    /// </summary>
    public long? MaxUsers { get; set; }

    public AccountMetaInformation? Application { get; set; }
}