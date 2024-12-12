namespace Atc.Hosting;

/// <summary>
/// The default service options used in <see cref="BackgroundScheduleServiceBase{T}"/>.
/// </summary>
public class DefaultBackgroundScheduleServiceOptions : IBackgroundScheduleServiceOptions
{
    /// <summary>
    /// Gets the cron expression for scheduling - the default value is "*/5 * * * *" (every 5 minutes).
    /// </summary>
    public string CronExpression { get; set; } = "*/5 * * * *";

    /// <summary>
    /// Gets or sets the name of the service.
    /// </summary>
    /// <value>
    /// The name of the service.
    /// </value>
    public string? ServiceName { get; set; }
}