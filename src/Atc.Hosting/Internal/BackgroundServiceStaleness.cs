namespace Atc.Hosting.Internal;

/// <summary>
/// A max staleness entry added through <see cref="ServiceCollectionExtensions.AddBackgroundServiceHealthService"/>,
/// for a service that isn't registered through <c>AddBackgroundService</c> or <c>AddBackgroundScheduleService</c>.
/// </summary>
/// <param name="ServiceName">The name of the service.</param>
/// <param name="MaxStalenessInSeconds">The max staleness applied to the service.</param>
internal sealed record BackgroundServiceStaleness(
    string ServiceName,
    ushort MaxStalenessInSeconds);