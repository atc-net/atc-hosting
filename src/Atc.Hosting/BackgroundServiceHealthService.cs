namespace Atc.Hosting;

/// <summary>
/// The default <see cref="IBackgroundServiceHealthService"/>, which keeps the running state of each service in memory.
/// </summary>
public sealed class BackgroundServiceHealthService : IBackgroundServiceHealthService
{
    /// <summary>
    /// The grace period in seconds to cope with internal mechanics between each invocation of a service.
    /// </summary>
    private const double GracePeriodInSeconds = 1.0;

    private readonly TimeProvider timeProvider;

    private readonly ConcurrentDictionary<string, (bool IsRunning, DateTimeOffset LastUpdated)> serviceStates = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ushort> maxStalenessInSeconds = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundServiceHealthService"/> class.
    /// </summary>
    /// <param name="timeProvider">The time provider used to timestamp running states and evaluate staleness.</param>
    public BackgroundServiceHealthService(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        this.timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public void SetMaxStalenessInSeconds(
        string serviceName,
        ushort seconds)
        => maxStalenessInSeconds[serviceName] = seconds == 0
            ? (ushort)1
            : seconds;

    /// <inheritdoc />
    public void SetRunningState(
        string serviceName,
        bool isRunning)
        => serviceStates[serviceName] = (isRunning, timeProvider.GetUtcNow());

    /// <inheritdoc />
    public bool IsServiceRunning(string serviceName)
    {
        if (!serviceStates.TryGetValue(serviceName, out var state) ||
            !state.IsRunning)
        {
            return false;
        }

        if (maxStalenessInSeconds.TryGetValue(serviceName, out var maxStaleness))
        {
            var secondsSinceLastUpdate = (timeProvider.GetUtcNow() - state.LastUpdated).TotalSeconds;

            return secondsSinceLastUpdate <= maxStaleness + GracePeriodInSeconds;
        }

        return true;
    }
}