namespace Atc.Hosting;

/// <summary>
/// The default service options used in <see cref="BackgroundScheduleServiceBase{T}"/>.
/// </summary>
public class DefaultBackgroundScheduleServiceOptions : IBackgroundScheduleServiceOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the service is enabled - the default value is true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets the cron expression for scheduling - the default value is "*/5 * * * *" (every 5 minutes).
    /// </summary>
    public string CronExpression { get; set; } = "*/5 * * * *";

    /// <summary>
    /// Gets or sets the maximum duration, in seconds, of a single unit of work before the service is reported as not running
    /// - the default value is null (no limit).
    /// </summary>
    public ushort? MaxWorkDurationSeconds { get; set; }

    /// <summary>
    /// Gets or sets the name of the service.
    /// </summary>
    /// <value>
    /// The name of the service.
    /// </value>
    public string? ServiceName { get; set; }

    /// <inheritdoc />
    public override string ToString()
        => $"{nameof(Enabled)}: {Enabled}, {nameof(CronExpression)}: {CronExpression}, {nameof(MaxWorkDurationSeconds)}: {MaxWorkDurationSeconds}, {nameof(ServiceName)}: {ServiceName}";
}