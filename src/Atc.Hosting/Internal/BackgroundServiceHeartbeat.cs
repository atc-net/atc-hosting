namespace Atc.Hosting.Internal;

/// <summary>
/// The single rule for how often a background service reports that it's alive, and the staleness that goes with it.
/// Both the base classes and the registration extension use it, so the two can't drift apart.
/// </summary>
internal static class BackgroundServiceHeartbeat
{
    /// <summary>
    /// The longest interval between two heartbeats, in seconds.
    /// </summary>
    internal const int MaxIntervalSeconds = 10;

    /// <summary>
    /// The heartbeat interval for schedule services, in seconds.
    /// </summary>
    internal const int ScheduleIntervalSeconds = 10;

    /// <summary>
    /// The staleness is this many heartbeat intervals, so two missed heartbeats are tolerated.
    /// </summary>
    internal const int StalenessMultiplier = 3;

    /// <summary>
    /// Gets the heartbeat interval for schedule services.
    /// </summary>
    internal static TimeSpan ScheduleInterval
        => TimeSpan.FromSeconds(ScheduleIntervalSeconds);

    /// <summary>
    /// Gets the heartbeat interval for an interval service: half of <see cref="IBackgroundServiceOptions.RepeatIntervalSeconds"/>,
    /// at least 1 second and at most <see cref="MaxIntervalSeconds"/>. Half the repeat interval keeps hosts that set
    /// the staleness to <see cref="IBackgroundServiceOptions.RepeatIntervalSeconds"/> themselves reporting the service as running.
    /// </summary>
    /// <param name="options">The service options.</param>
    /// <returns>The heartbeat interval.</returns>
    internal static TimeSpan GetInterval(IBackgroundServiceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return TimeSpan.FromSeconds(Math.Clamp(options.RepeatIntervalSeconds / 2, 1, MaxIntervalSeconds));
    }

    /// <summary>
    /// Gets the max staleness that matches a heartbeat interval.
    /// </summary>
    /// <param name="interval">The heartbeat interval.</param>
    /// <returns>The max staleness in seconds.</returns>
    internal static ushort GetMaxStalenessInSeconds(TimeSpan interval)
        => (ushort)Math.Min(ushort.MaxValue, Math.Ceiling(interval.TotalSeconds) * StalenessMultiplier);
}