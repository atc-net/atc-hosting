namespace Atc.Hosting;

public sealed class BackgroundServiceHealthService : IBackgroundServiceHealthService
{
    /// <summary>
    /// The grace period in seconds to cope with internal mechanics between each invocation of a service.
    /// </summary>
    private const double GracePeriodInSeconds = 1.0;

    private readonly ITimeProvider timeProvider;

    private readonly ConcurrentDictionary<string, (bool IsRunning, DateTime LastUpdated)> serviceStates = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ushort> maxStalenessInSeconds = new(StringComparer.Ordinal);

    public BackgroundServiceHealthService(
        ITimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;
    }

    public void SetMaxStalenessInSeconds(
        string serviceName,
        ushort seconds)
        => maxStalenessInSeconds[serviceName] = seconds == 0
            ? (ushort)1
            : seconds;

    public void SetRunningState(
        string serviceName,
        bool isRunning)
        => serviceStates[serviceName] = (isRunning, timeProvider.UtcNow);

    public bool IsServiceRunning(
        string serviceName)
    {
        if (!serviceStates.TryGetValue(serviceName, out var state)
            || !maxStalenessInSeconds.TryGetValue(serviceName, out var maxStaleness))
        {
            return false;
        }

        var dateTimeDiff = state.LastUpdated.DateTimeDiff(timeProvider.UtcNow, DateTimeDiffCompareType.Seconds);

        return state.IsRunning &&
            (dateTimeDiff <= maxStaleness + GracePeriodInSeconds);
    }
}