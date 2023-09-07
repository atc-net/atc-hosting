namespace Atc.Hosting;

/// <summary>
/// Provides methods to manage and monitor the health of background services.
/// </summary>
public interface IBackgroundServiceHealthService
{
    /// <summary>
    /// Sets the maximum allowed staleness duration for a given service.
    /// </summary>
    /// <param name="serviceName">Name of the service.</param>
    /// <param name="seconds">Max allowed staleness in seconds.</param>
    void SetMaxStalenessInSeconds(
        string serviceName,
        ushort seconds);

    /// <summary>
    /// Updates the running state of a given service.
    /// </summary>
    /// <param name="serviceName">Name of the service.</param>
    /// <param name="isRunning">Current running state of the service.</param>
    void SetRunningState(
        string serviceName,
        bool isRunning);

    /// <summary>
    /// Checks if a given service is running.
    /// </summary>
    /// <param name="serviceName">Name of the service.</param>
    /// <returns>True if the service is running; otherwise, false.</returns>
    bool IsServiceRunning(
        string serviceName);
}