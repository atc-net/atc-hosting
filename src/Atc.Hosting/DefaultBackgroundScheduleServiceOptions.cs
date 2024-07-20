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
}