namespace Atc.Hosting.HealthChecks;

/// <summary>
/// Reports whether one background service is running, using <see cref="IBackgroundServiceHealthService"/>.
/// </summary>
/// <remarks>
/// The service name is the name of the health check registration, so one registration is needed per service.
/// Use <see cref="HealthChecksBuilderExtensions"/> to add them.
/// </remarks>
/// <param name="healthService">The health service the background services report to.</param>
public sealed class BackgroundServiceHealthCheck(IBackgroundServiceHealthService healthService) : IHealthCheck
{
    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var name = context.Registration.Name;

        return Task.FromResult(
            healthService.IsServiceRunning(name)
                ? HealthCheckResult.Healthy($"Background service '{name}' is healthy")
                : new HealthCheckResult(context.Registration.FailureStatus, $"Background service '{name}' is unhealthy."));
    }
}