namespace Atc.Hosting;

public abstract class BackgroundScheduleServiceBase<T> : BackgroundService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundScheduleServiceBase{T}" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="backgroundScheduleServiceOptions">The background schedule service options.</param>
    /// <param name="healthService">The background service health service.</param>
    protected BackgroundScheduleServiceBase(
        ILogger<T> logger,
        IBackgroundScheduleServiceOptions backgroundScheduleServiceOptions,
        IBackgroundServiceHealthService healthService)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ServiceOptions = backgroundScheduleServiceOptions ?? throw new ArgumentNullException(nameof(backgroundScheduleServiceOptions));
        this.healthService = healthService ?? throw new ArgumentNullException(nameof(healthService));
        ServiceName = typeof(T).Name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundScheduleServiceBase{T}" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="backgroundScheduleServiceOptions">The background schedule service options.</param>
    protected BackgroundScheduleServiceBase(
        ILogger<T> logger,
        IBackgroundScheduleServiceOptions backgroundScheduleServiceOptions)
        : this(
            logger,
            backgroundScheduleServiceOptions,
            NullBackgroundServiceHealthService.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundScheduleServiceBase{T}" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="healthService">The background service health service.</param>
    protected BackgroundScheduleServiceBase(
        ILogger<T> logger,
        IBackgroundServiceHealthService healthService)
        : this(
            logger,
            new DefaultBackgroundScheduleServiceOptions(),
            healthService)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundScheduleServiceBase{T}" /> class.
    /// </summary>
    /// <param name="backgroundScheduleServiceOptions">The background schedule service options.</param>
    /// <param name="healthService">The background service health service.</param>
    protected BackgroundScheduleServiceBase(
        IBackgroundScheduleServiceOptions backgroundScheduleServiceOptions,
        IBackgroundServiceHealthService healthService)
        : this(
            NullLogger<T>.Instance,
            backgroundScheduleServiceOptions,
            healthService)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundScheduleServiceBase{T}" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    protected BackgroundScheduleServiceBase(
        ILogger<T> logger)
        : this(
            logger,
            new DefaultBackgroundScheduleServiceOptions(),
            NullBackgroundServiceHealthService.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundScheduleServiceBase{T}" /> class.
    /// </summary>
    /// <param name="backgroundScheduleServiceOptions">The background schedule service options.</param>
    protected BackgroundScheduleServiceBase(
        IBackgroundScheduleServiceOptions backgroundScheduleServiceOptions)
        : this(
            NullLogger<T>.Instance,
            backgroundScheduleServiceOptions,
            NullBackgroundServiceHealthService.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundScheduleServiceBase{T}" /> class.
    /// </summary>
    /// <param name="healthService">The background service health service.</param>
    protected BackgroundScheduleServiceBase(
        IBackgroundServiceHealthService healthService)
        : this(
            NullLogger<T>.Instance,
            new DefaultBackgroundScheduleServiceOptions(),
            healthService)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundScheduleServiceBase{T}" /> class.
    /// </summary>
    protected BackgroundScheduleServiceBase()
        : this(
            NullLogger<T>.Instance,
            new DefaultBackgroundScheduleServiceOptions(),
            NullBackgroundServiceHealthService.Instance)
    {
    }

    /// <summary>
    /// Gets the name of the service.
    /// </summary>
    public string ServiceName { get; }

    /// <summary>
    /// Gets the service options.
    /// </summary>
    public IBackgroundScheduleServiceOptions ServiceOptions { get; }

    /// <summary>
    /// Logger for this service
    /// </summary>
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "OK.")]
    [SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "OK.")]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "OK.")]
    protected readonly ILogger<T> logger;

    /// <summary>
    /// Get the Background Service Health Service.
    /// </summary>
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "OK.")]
    [SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "OK.")]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "OK.")]
    protected readonly IBackgroundServiceHealthService healthService;

    /// <summary>
    /// Work method run based on <see cref="IBackgroundScheduleServiceOptions.CronExpression" />.
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
    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK - by design.")]
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        // Awaiting Task.Yield() transitions to asynchronous operation immediately.
        // This allows startup to continue without waiting.
        await Task.Yield();

        logger.LogBackgroundServiceStarted(ServiceName, ServiceOptions.CronExpression);
        healthService.SetRunningState(ServiceName, isRunning: true);

        try
        {
            var cronExpression = CronExpression.Parse(ServiceOptions.CronExpression);
            var nextOccurrence = cronExpression.GetNextOccurrence(DateTime.UtcNow);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (nextOccurrence.HasValue)
                {
                    var delay = nextOccurrence.Value - DateTime.UtcNow;

                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
                    }

                    try
                    {
                        await DoWorkAsync(stoppingToken).ConfigureAwait(false);
                        healthService.SetRunningState(ServiceName, isRunning: true);
                    }
                    catch (Exception ex)
                    {
                        await OnExceptionAsync(ex, stoppingToken).ConfigureAwait(false);

                        stoppingToken.ThrowIfCancellationRequested();

                        logger.LogBackgroundServiceRetrying(
                            ServiceName,
                            ServiceOptions.CronExpression,
                            ex);
                    }

                    nextOccurrence = cronExpression.GetNextOccurrence(DateTime.UtcNow);
                }
                else
                {
                    nextOccurrence = cronExpression.GetNextOccurrence(DateTime.UtcNow);
                }
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
            healthService.SetRunningState(ServiceName, isRunning: false);
        }
    }
}