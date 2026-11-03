namespace Atc.Hosting;

/// <summary>
/// The options shared by all background services, used by <see cref="IBackgroundServiceOptions"/>
/// and <see cref="IBackgroundScheduleServiceOptions"/>.
/// </summary>
public interface IBackgroundServiceBaseOptions
{
    /// <summary>
    /// Gets or sets the name of the service.
    /// When <see langword="null"/>, the name of the service type is used.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the service is enabled.
    /// </summary>
    /// <remarks>
    /// When <see langword="false"/>, the service does no work: <see cref="BackgroundServiceBase{T}"/> and
    /// <see cref="BackgroundScheduleServiceBase{T}"/> return from <c>ExecuteAsync</c> immediately and don't report a running state.
    /// When the service is registered through <c>AddBackgroundService</c> or <c>AddBackgroundScheduleService</c>,
    /// it isn't registered as a hosted service at all.
    /// </remarks>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the maximum duration, in seconds, of a single unit of work before the service is reported as not running.
    /// </summary>
    /// <remarks>
    /// When a single <c>DoWorkAsync</c> run lasts longer than this, the service stops reporting itself as running until the run completes,
    /// so a hung run makes the service unhealthy. The run itself isn't cancelled.
    /// When <see langword="null"/>, there is no limit, and a hung run keeps reporting the service as running.
    /// </remarks>
    public ushort? MaxWorkDurationSeconds { get; set; }
}