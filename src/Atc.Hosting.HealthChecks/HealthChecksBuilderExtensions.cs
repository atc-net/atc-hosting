namespace Atc.Hosting.HealthChecks;

/// <summary>
/// Extensions for adding one <see cref="BackgroundServiceHealthCheck"/> per background service.
/// </summary>
public static class HealthChecksBuilderExtensions
{
    /// <summary>
    /// Adds one health check, named after the service, per service registered through
    /// <see cref="ServiceCollectionExtensions.AddBackgroundService{TService, TOptions}(IServiceCollection, TOptions)"/> or
    /// <see cref="ServiceCollectionExtensions.AddBackgroundScheduleService{TService, TOptions}(IServiceCollection, TOptions)"/>.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="failureStatus">The status to report when a service isn't running. The default is <see cref="HealthStatus.Unhealthy"/>.</param>
    /// <param name="tags">The tags for the health checks.</param>
    /// <param name="timeout">The timeout for the health checks.</param>
    /// <returns>The health checks builder, for chaining.</returns>
    /// <remarks>
    /// <para>
    /// The services are read when the health checks are first used, not when this method is called,
    /// so it can be called before or after the <c>AddBackgroundService</c> and <c>AddBackgroundScheduleService</c> calls.
    /// </para>
    /// <para>
    /// Disabled services get no check. Services registered with plain <c>AddHostedService&lt;T&gt;()</c> don't either,
    /// even if their names were passed to <see cref="ServiceCollectionExtensions.AddBackgroundServiceHealthService"/>.
    /// Use <see cref="AddBackgroundServices(IHealthChecksBuilder, IEnumerable{string}, HealthStatus?, IEnumerable{string}?, TimeSpan?)"/> for those.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code><![CDATA[
    /// services
    ///     .AddHealthChecks()
    ///     .AddBackgroundServices(tags: ["dependency"]);
    ///
    /// services.AddBackgroundService<TimeFileWorker, TimeFileWorkerOptions>(
    ///     configuration.GetSection(TimeFileWorkerOptions.SectionName));
    /// ]]></code>
    /// </example>
    public static IHealthChecksBuilder AddBackgroundServices(
        this IHealthChecksBuilder builder,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // The configure step runs later, so take a copy of the tags in case the caller changes the collection.
        var tagsCopy = tags?.ToArray();

        builder.Services
            .AddOptions<HealthCheckServiceOptions>()
            .Configure<IEnumerable<BackgroundServiceRegistration>>((options, registrations) =>
            {
                foreach (var registration in registrations)
                {
                    options.Registrations.Add(CreateRegistration(registration.ServiceName, failureStatus, tagsCopy, timeout));
                }
            });

        return builder;
    }

    /// <summary>
    /// Adds one health check per given service name.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="names">The names the services report under.</param>
    /// <param name="failureStatus">The status to report when a service isn't running. The default is <see cref="HealthStatus.Unhealthy"/>.</param>
    /// <param name="tags">The tags for the health checks.</param>
    /// <param name="timeout">The timeout for the health checks.</param>
    /// <returns>The health checks builder, for chaining.</returns>
    /// <remarks>
    /// For hosts that register their services with plain <c>AddHostedService&lt;T&gt;()</c>.
    /// The host has to register <see cref="IBackgroundServiceHealthService"/> itself, e.g. with
    /// <see cref="ServiceCollectionExtensions.AddBackgroundServiceHealthService"/>.
    /// </remarks>
    public static IHealthChecksBuilder AddBackgroundServices(
        this IHealthChecksBuilder builder,
        IEnumerable<string> names,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(names);

        foreach (var name in names)
        {
            builder.Add(CreateRegistration(name, failureStatus, tags, timeout));
        }

        return builder;
    }

    private static HealthCheckRegistration CreateRegistration(
        string name,
        HealthStatus? failureStatus,
        IEnumerable<string>? tags,
        TimeSpan? timeout)
        => new(
            name,
            serviceProvider => ActivatorUtilities.GetServiceOrCreateInstance<BackgroundServiceHealthCheck>(serviceProvider),
            failureStatus,
            tags,
            timeout);
}