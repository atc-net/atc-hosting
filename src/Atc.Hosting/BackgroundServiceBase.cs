namespace Atc.Hosting;

/// <summary>
/// BackgroundService provides consistent logging (including a logger enriched with the type of the service).
/// The <see cref="DoWorkAsync"/> method is called indefinitely, so long as it is supposed to.
/// </summary>
/// <typeparam name="T">The service type.</typeparam>
public abstract class BackgroundServiceBase<T> : BackgroundService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundServiceBase{T}" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="backgroundServiceOptions">The background service options.</param>
    protected BackgroundServiceBase(
        ILogger<T> logger,
        IBackgroundServiceOptions backgroundServiceOptions)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.ServiceOptions = backgroundServiceOptions ?? throw new ArgumentNullException(nameof(backgroundServiceOptions));
        this.ServiceName = typeof(T).Name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundServiceBase{T}" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    protected BackgroundServiceBase(
        ILogger<T> logger)
        : this(
            logger,
            new DefaultBackgroundServiceOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundServiceBase{T}" /> class.
    /// </summary>
    /// <param name="backgroundServiceOptions">The background service options.</param>
    protected BackgroundServiceBase(
        IBackgroundServiceOptions backgroundServiceOptions)
        : this(
            NullLogger<T>.Instance,
            backgroundServiceOptions)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundServiceBase{T}" /> class.
    /// </summary>
    protected BackgroundServiceBase()
        : this(
            NullLogger<T>.Instance,
            new DefaultBackgroundServiceOptions())
    {
    }

    /// <summary>
    /// Gets the name of the service.
    /// </summary>
    public string ServiceName { get; }

    /// <summary>
    /// Gets the service options.
    /// </summary>
    public IBackgroundServiceOptions ServiceOptions { get; }

    /// <summary>
    /// Logger for this service
    /// </summary>
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "OK.")]
    [SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "OK.")]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "OK.")]
    protected readonly ILogger<T> logger;

    /// <summary>
    /// Work method run based on <see cref="IBackgroundServiceOptions.RepeatIntervalSeconds" />.
    /// <br/><br/>
    /// Exceptions thrown here are turned into alerts / logs with severity of <see cref="LogLevel.Warning" />.
    /// </summary>
    /// <param name="stoppingToken">The stopping token.</param>
    /// <returns>The Task of the workload.</returns>
    public abstract Task DoWorkAsync(
        CancellationToken stoppingToken);

    /// <summary>
    /// Hook for manual handling of exceptions before wait and retry.
    /// <br/><br/>
    /// Default behavior with no override is wait and retry.
    /// <br/><br/>
    /// You can safely invoke <see cref="BackgroundService.StopAsync(CancellationToken)"/> here to initiate a graceful shutdown of the worker service.
    /// </summary>
    /// <param name="exception">The unhandled exception</param>
    /// <param name="stoppingToken">The stopping token</param>
    /// <returns>The Task of the exception handling</returns>
    protected virtual Task OnExceptionAsync(
        Exception exception,
        CancellationToken stoppingToken)
        => Task.CompletedTask;

    /// <inheritdoc />
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "OK - by design.")]
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        // Awaiting Task.Yield() transitions to asynchronous operation immediately.
        // This allows startup to continue without waiting.
        await Task.Yield();

        logger.LogBackgroundServiceStarted(ServiceName, ServiceOptions.RepeatIntervalSeconds);

        try
        {
            await Task
                .Delay(ServiceOptions.StartupDelaySeconds * 1_000, stoppingToken)
                .ConfigureAwait(false);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DoWorkAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await OnExceptionAsync(ex, stoppingToken).ConfigureAwait(false);

                    stoppingToken.ThrowIfCancellationRequested();

                    logger.LogBackgroundServiceRetrying(
                        ServiceName,
                        ServiceOptions.RepeatIntervalSeconds,
                        ex);
                }

                await Task
                    .Delay(ServiceOptions.RepeatIntervalSeconds * 1_000, stoppingToken)
                    .ConfigureAwait(false);
            }
        }
        catch when (stoppingToken.IsCancellationRequested)
        {
            logger.LogBackgroundServiceCancelled(ServiceName);
        }
        catch (Exception ex)
        {
            logger.LogBackgroundServiceUnhandledException(ServiceName, ex);
        }
        finally
        {
            logger.LogBackgroundServiceStopped(ServiceName);
        }
    }
}