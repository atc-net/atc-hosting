// ReSharper disable CheckNamespace
namespace Atc.Hosting;

/// <summary>
/// Extensions for registering background services, their options and their health wiring in one call.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="BackgroundServiceBase{T}"/> service, binding its options from a configuration section.
    /// </summary>
    /// <typeparam name="TService">The hosted service type.</typeparam>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration section to bind the options from.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <remarks>
    /// <para>
    /// The options are bound onto the defaults of <c>new TOptions()</c>. A missing section isn't an error; the defaults are used.
    /// To bind and validate the options yourself, e.g. to fail when a required section is missing,
    /// use <see cref="AddBackgroundService{TService, TOptions}(IServiceCollection, TOptions)"/>.
    /// </para>
    /// <para>
    /// The options are always registered as <typeparamref name="TOptions"/> and <see cref="IOptions{TOptions}"/>,
    /// even when the service is disabled, so other code that injects them keeps working. <see cref="IOptionsMonitor{TOptions}"/>
    /// and <see cref="IOptionsSnapshot{TOptions}"/> resolve the same instance. The values are bound once, here: they don't reload,
    /// and <c>Configure&lt;TOptions&gt;</c> calls are ignored. Registered <see cref="IValidateOptions{TOptions}"/> validators do run.
    /// </para>
    /// <para>
    /// When <see cref="IBackgroundServiceBaseOptions.Enabled"/> is <see langword="true"/>, the method also registers the hosted service,
    /// a <see cref="BackgroundServiceRegistration"/>, and the health service (see <see cref="AddBackgroundServiceHealthService"/>),
    /// which applies the service's max staleness automatically.
    /// When it's <see langword="false"/>, nothing else is registered.
    /// </para>
    /// <para>
    /// The service name is <see cref="IBackgroundServiceBaseOptions.ServiceName"/>. When that isn't set, it's the name the service reports under:
    /// the <c>T</c> of its <see cref="BackgroundServiceBase{T}"/> or <see cref="BackgroundScheduleServiceBase{T}"/>, which is normally <typeparamref name="TService"/>.
    /// A service without one of those base classes uses the name of <typeparamref name="TService"/>.
    /// Registering two services with the same name, or the same <typeparamref name="TService"/> twice, throws <see cref="InvalidOperationException"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code><![CDATA[
    /// services.AddBackgroundService<TimeFileWorker, TimeFileWorkerOptions>(
    ///     configuration.GetSection(TimeFileWorkerOptions.SectionName));
    /// ]]></code>
    /// </example>
    public static IServiceCollection AddBackgroundService<TService, TOptions>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TService : class, IHostedService
        where TOptions : class, IBackgroundServiceOptions, new()
        => services.AddBackgroundService<TService, TOptions>(Bind<TOptions>(configuration));

    /// <summary>
    /// Registers a <see cref="BackgroundServiceBase{T}"/> service with options you have already bound and validated.
    /// </summary>
    /// <typeparam name="TService">The hosted service type.</typeparam>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The options.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <remarks>
    /// Registers the same things as <see cref="AddBackgroundService{TService, TOptions}(IServiceCollection, IConfiguration)"/>,
    /// without binding anything. Use it when the options are bound and validated elsewhere.
    /// A host that registers its own <see cref="IBackgroundServiceHealthService"/> first keeps it,
    /// and then has to apply the staleness itself.
    /// </remarks>
    public static IServiceCollection AddBackgroundService<TService, TOptions>(
        this IServiceCollection services,
        TOptions options)
        where TService : class, IHostedService
        where TOptions : class, IBackgroundServiceOptions
    {
        ArgumentNullException.ThrowIfNull(options);

        return AddBackgroundServiceCore<TService, TOptions>(
            services,
            options,
            BackgroundServiceHeartbeat.GetMaxStalenessInSeconds(BackgroundServiceHeartbeat.GetInterval(options)));
    }

    /// <summary>
    /// Registers a <see cref="BackgroundScheduleServiceBase{T}"/> service, binding its options from a configuration section.
    /// </summary>
    /// <typeparam name="TService">The hosted service type.</typeparam>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration section to bind the options from.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <remarks>
    /// Works like <see cref="AddBackgroundService{TService, TOptions}(IServiceCollection, IConfiguration)"/>.
    /// The max staleness follows the schedule services' heartbeat, not the cron expression,
    /// so the service stays healthy while it waits for its next occurrence.
    /// </remarks>
    /// <example>
    /// <code><![CDATA[
    /// services.AddBackgroundScheduleService<TimeFileScheduleWorker, TimeFileScheduleWorkerOptions>(
    ///     configuration.GetSection(TimeFileScheduleWorkerOptions.SectionName));
    /// ]]></code>
    /// </example>
    public static IServiceCollection AddBackgroundScheduleService<TService, TOptions>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TService : class, IHostedService
        where TOptions : class, IBackgroundScheduleServiceOptions, new()
        => services.AddBackgroundScheduleService<TService, TOptions>(Bind<TOptions>(configuration));

    /// <summary>
    /// Registers a <see cref="BackgroundScheduleServiceBase{T}"/> service with options you have already bound and validated.
    /// </summary>
    /// <typeparam name="TService">The hosted service type.</typeparam>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The options.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <remarks>
    /// Registers the same things as <see cref="AddBackgroundScheduleService{TService, TOptions}(IServiceCollection, IConfiguration)"/>,
    /// without binding anything. A host that registers its own <see cref="IBackgroundServiceHealthService"/> first keeps it,
    /// and then has to apply the staleness itself.
    /// </remarks>
    public static IServiceCollection AddBackgroundScheduleService<TService, TOptions>(
        this IServiceCollection services,
        TOptions options)
        where TService : class, IHostedService
        where TOptions : class, IBackgroundScheduleServiceOptions
        => AddBackgroundServiceCore<TService, TOptions>(
            services,
            options,
            BackgroundServiceHeartbeat.GetMaxStalenessInSeconds(BackgroundServiceHeartbeat.ScheduleInterval));

    /// <summary>
    /// Registers <see cref="IBackgroundServiceHealthService"/> and a <see cref="TimeProvider"/>, and applies max staleness for the given services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="maxStaleness">The max staleness, in seconds, per service name.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <remarks>
    /// <para>
    /// For hosts that register their services with plain <c>AddHostedService&lt;T&gt;()</c>.
    /// Services registered through <c>AddBackgroundService</c> or <c>AddBackgroundScheduleService</c> don't need it; those calls
    /// register the health service and apply the staleness themselves.
    /// </para>
    /// <para>
    /// The health service and the time provider are registered with <c>TryAdd</c>, so calling this several times,
    /// or together with <c>AddBackgroundService</c>, is safe. A host that registers its own
    /// <see cref="IBackgroundServiceHealthService"/> first keeps it, and then has to apply the staleness itself.
    /// A service name that is already registered throws <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// Names passed here aren't <see cref="BackgroundServiceRegistration"/>s, so the parameterless
    /// <c>AddBackgroundServices()</c> health checks extension doesn't pick them up; pass the names to it explicitly.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code><![CDATA[
    /// // Before:
    /// services.AddSingleton<IBackgroundServiceHealthService, BackgroundServiceHealthService>(s =>
    /// {
    ///     var healthService = new BackgroundServiceHealthService(s.GetRequiredService<TimeProvider>());
    ///     var options = s.GetRequiredService<IOptions<TimeFileWorkerOptions>>().Value;
    ///     healthService.SetMaxStalenessInSeconds(nameof(TimeFileWorker), options.RepeatIntervalSeconds);
    ///     return healthService;
    /// });
    ///
    /// // After:
    /// services.AddBackgroundServiceHealthService(
    ///     (nameof(TimeFileWorker), timeFileWorkerOptions.RepeatIntervalSeconds));
    /// ]]></code>
    /// </example>
    public static IServiceCollection AddBackgroundServiceHealthService(
        this IServiceCollection services,
        params (string ServiceName, ushort MaxStalenessInSeconds)[] maxStaleness)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(maxStaleness);

        foreach (var (serviceName, maxStalenessInSeconds) in maxStaleness)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                throw new ArgumentException("A service name can't be null or empty.", nameof(maxStaleness));
            }

            EnsureUnique(services, serviceName, serviceType: null);
            services.AddSingleton(new BackgroundServiceStaleness(serviceName, maxStalenessInSeconds));
        }

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IBackgroundServiceHealthService>(CreateHealthService);

        return services;
    }

    private static TOptions Bind<TOptions>(IConfiguration configuration)
        where TOptions : class, new()
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new TOptions();
        configuration.Bind(options);

        return options;
    }

    private static string GetDefaultServiceName(Type serviceType)
    {
        // The base classes name a service after their T, which isn't TService when TService derives from another worker.
        for (var type = serviceType; type is not null; type = type.BaseType)
        {
            if (!type.IsGenericType)
            {
                continue;
            }

            var definition = type.GetGenericTypeDefinition();

            if (definition == typeof(BackgroundServiceBase<>) ||
                definition == typeof(BackgroundScheduleServiceBase<>))
            {
                return type.GetGenericArguments()[0].Name;
            }
        }

        return serviceType.Name;
    }

    private static IServiceCollection AddBackgroundServiceCore<TService, TOptions>(
        IServiceCollection services,
        TOptions options,
        ushort maxStalenessInSeconds)
        where TService : class, IHostedService
        where TOptions : class, IBackgroundServiceBaseOptions
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        var serviceName = options.ServiceName ?? GetDefaultServiceName(typeof(TService));

        // The options are registered as a singleton per type, so two services sharing a type would both get the last one's options.
        EnsureOptionsTypeIsUnused(services, typeof(TOptions));
        services.AddSingleton(new BackgroundServiceOptionsUsage(typeof(TOptions), serviceName));

        // The same instance for TOptions, IOptions, IOptionsMonitor and IOptionsSnapshot.
        // The three interfaces all go through the factory, so any registered validators run for them.
        services.AddOptions();
        services.AddSingleton(options);
        services.AddTransient<IOptionsFactory<TOptions>, RegisteredOptionsFactory<TOptions>>();

        if (!options.Enabled)
        {
            return services;
        }

        EnsureUnique(services, serviceName, typeof(TService));

        services.AddSingleton(new BackgroundServiceRegistration(serviceName, typeof(TService), maxStalenessInSeconds));
        services.AddHostedService<TService>();
        services.AddBackgroundServiceHealthService();

        return services;
    }

    private static void EnsureUnique(
        IServiceCollection services,
        string serviceName,
        Type? serviceType)
    {
        foreach (var descriptor in services)
        {
            // ImplementationInstance throws for keyed descriptors, and a keyed registration isn't ours anyway.
            if (descriptor.IsKeyedService)
            {
                continue;
            }

            switch (descriptor.ImplementationInstance)
            {
                case BackgroundServiceRegistration registration when string.Equals(registration.ServiceName, serviceName, StringComparison.Ordinal):
                case BackgroundServiceStaleness staleness when string.Equals(staleness.ServiceName, serviceName, StringComparison.Ordinal):
                    throw new InvalidOperationException($"A background service named '{serviceName}' is already registered.");

                case BackgroundServiceRegistration registration when registration.ServiceType == serviceType:
                    throw new InvalidOperationException(
                        $"The background service type '{serviceType.FullName}' is already registered as '{registration.ServiceName}'. A hosted service type can only be registered once.");
            }
        }
    }

    private static void EnsureOptionsTypeIsUnused(
        IServiceCollection services,
        Type optionsType)
    {
        var usage = services
            .Where(d => !d.IsKeyedService)
            .Select(d => d.ImplementationInstance)
            .OfType<BackgroundServiceOptionsUsage>()
            .FirstOrDefault(u => u.OptionsType == optionsType);

        if (usage is not null)
        {
            throw new InvalidOperationException(
                $"The options type '{optionsType.FullName}' is already used by the background service '{usage.ServiceName}'. " +
                "Each background service needs its own options type, because the options are registered as a singleton per type.");
        }
    }

    private static BackgroundServiceHealthService CreateHealthService(
        IServiceProvider serviceProvider)
    {
        var healthService = new BackgroundServiceHealthService(serviceProvider.GetRequiredService<TimeProvider>());

        foreach (var registration in serviceProvider.GetServices<BackgroundServiceRegistration>())
        {
            healthService.SetMaxStalenessInSeconds(registration.ServiceName, registration.MaxStalenessInSeconds);
        }

        foreach (var staleness in serviceProvider.GetServices<BackgroundServiceStaleness>())
        {
            healthService.SetMaxStalenessInSeconds(staleness.ServiceName, staleness.MaxStalenessInSeconds);
        }

        return healthService;
    }
}