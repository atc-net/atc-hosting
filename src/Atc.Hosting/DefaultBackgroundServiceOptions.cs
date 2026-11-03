namespace Atc.Hosting;

/// <summary>
/// The default service options used in <see cref="BackgroundServiceBase{T}"/>.
/// </summary>
public class DefaultBackgroundServiceOptions : IBackgroundServiceOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the service is enabled - the default value is true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Defines the delay period before the first unit of work - the default value is 1.
    /// </summary>
    public ushort StartupDelaySeconds { get; set; } = 1;

    /// <summary>
    /// Defines the period between the end of one unit of work and the start of the next - the default value is 30.
    /// </summary>
    public ushort RepeatIntervalSeconds { get; set; } = 30;

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
        => $"{nameof(Enabled)}: {Enabled}, {nameof(StartupDelaySeconds)}: {StartupDelaySeconds}, {nameof(RepeatIntervalSeconds)}: {RepeatIntervalSeconds}, {nameof(MaxWorkDurationSeconds)}: {MaxWorkDurationSeconds}, {nameof(ServiceName)}: {ServiceName}";
}