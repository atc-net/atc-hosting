namespace Atc.Hosting;

/// <summary>
/// Records one enabled background service registered through
/// <see cref="ServiceCollectionExtensions.AddBackgroundService{TService, TOptions}(IServiceCollection, TOptions)"/> or
/// <see cref="ServiceCollectionExtensions.AddBackgroundScheduleService{TService, TOptions}(IServiceCollection, TOptions)"/>.
/// </summary>
/// <remarks>
/// The registration is added to the service collection as a singleton instance. The health service and the
/// <c>Atc.Hosting.HealthChecks</c> package read it, so the hosted service, its staleness and its health check come from one call.
/// Disabled services have no registration.
/// </remarks>
/// <param name="ServiceName">The name of the service: <see cref="IBackgroundServiceBaseOptions.ServiceName"/>, or the service type name when that isn't set.</param>
/// <param name="ServiceType">The hosted service type.</param>
/// <param name="MaxStalenessInSeconds">The max staleness applied to the service in <see cref="IBackgroundServiceHealthService"/>.</param>
public sealed record BackgroundServiceRegistration(
    string ServiceName,
    Type ServiceType,
    ushort MaxStalenessInSeconds);