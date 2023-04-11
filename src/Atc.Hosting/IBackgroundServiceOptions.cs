namespace Atc.Hosting;

/// <summary>
/// The interface definition for BackgroundServiceOptions used in <see cref="BackgroundServiceBase{T}"/>.
/// </summary>
public interface IBackgroundServiceOptions
{
    /// <summary>
    /// Defines the delay period before the first unit of work.
    /// </summary>
    ushort StartupDelaySeconds { get; set; }

    /// <summary>
    /// Defines the retry count.
    /// </summary>
    ushort RetryCount { get; set; }

    /// <summary>
    /// Defines the period between the end of one unit of work and the start of the next.
    /// </summary>
    ushort RepeatIntervalSeconds { get; set; }
}