namespace Atc.Hosting.Internal;

internal sealed class NullBackgroundServiceHealthService : IBackgroundServiceHealthService
{
    /// <summary>
    /// Returns static instance of <see cref="NullBackgroundServiceHealthService"/>
    /// </summary>
    public static NullBackgroundServiceHealthService Instance => new NullBackgroundServiceHealthService();

    private NullBackgroundServiceHealthService()
    {
    }

    public bool IsServiceRunning(string serviceName) => false;

    public void SetMaxStalenessInSeconds(string serviceName, ushort seconds)
    {
        // Method intentionally left empty.
    }

    public void SetRunningState(string serviceName, bool isRunning)
    {
        // Method intentionally left empty.
    }
}