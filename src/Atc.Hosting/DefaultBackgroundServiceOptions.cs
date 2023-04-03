namespace Atc.Hosting;

/// <summary>
/// The default service options used in <see cref="BackgroundServiceBase{T}"/>.
/// </summary>
public class DefaultBackgroundServiceOptions : IBackgroundServiceOptions
{
    /// <summary>
    /// Defines the delay period before the first unit of work - the default value is 1.
    /// </summary>
    public ushort StartupDelaySeconds { get; set; } = 1;

    /// <summary>
    /// Defines the retry count - the default value is 3.
    /// </summary>
    public ushort RetryCount { get; set; } = 3;

    /// <summary>
    /// Defines the period between the end of one unit of work and the start of the next - the default value is 30.
    /// </summary>
    public ushort RepeatIntervalSeconds { get; set; } = 30;

    /// <inheritdoc />
    public override string ToString()
        => $"{nameof(StartupDelaySeconds)}: {StartupDelaySeconds}, {nameof(RetryCount)}: {RetryCount}, {nameof(RepeatIntervalSeconds)}: {RepeatIntervalSeconds}";
}