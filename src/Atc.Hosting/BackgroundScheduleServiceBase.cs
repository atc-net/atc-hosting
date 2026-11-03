namespace Atc.Hosting;

public abstract class BackgroundScheduleServiceBase<T> : BackgroundService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundScheduleServiceBase{T}" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="backgroundScheduleServiceOptions">The background schedule service options.</param>
    /// <param name="healthService">The background service health service.</param>
    /// <param name="timeProvider">The time provider used for all delays and clock reads; <see cref="TimeProvider.System"/> when <see langword="null"/>.</param>
    protected BackgroundScheduleServiceBase(
        ILogger<T> logger,
        IBackgroundScheduleServiceOptions backgroundScheduleServiceOptions,
        IBackgroundServiceHealthService healthService,
        TimeProvider? timeProvider = null)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ServiceOptions = backgroundScheduleServiceOptions ?? throw new ArgumentNullException(nameof(backgroundScheduleServiceOptions));
        this.healthService = healthService ?? throw new ArgumentNullException(nameof(healthService));
        TimeProvider = timeProvider ?? TimeProvider.System;
        ServiceName = backgroundScheduleServiceOptions.ServiceName ?? typeof(T).Name;
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
    protected BackgroundScheduleServiceBase(ILogger<T> logger)
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
    /// Gets the time provider used for all delays and clock reads in this service.
    /// </summary>
    protected TimeProvider TimeProvider { get; }

    /// <summary>
    /// Logger for this service
    /// </summary>
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "OK.")]
    [SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "OK.")]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "OK.")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1304:Non-private readonly fields should begin with upper-case letter", Justification = "OK - protected fields are part of the public API.")]
    protected readonly ILogger<T> logger;

    /// <summary>
    /// Get the Background Service Health Service.
    /// </summary>
    /// <remarks>
    /// The base class reports the running state itself (a heartbeat, plus before and after each unit of work), so a worker
    /// doesn't need to call <see cref="IBackgroundServiceHealthService.SetRunningState"/>. A manual "running" report overrides
    /// <see cref="IBackgroundServiceBaseOptions.MaxWorkDurationSeconds"/> and <see cref="IBackgroundServiceBaseOptions.MaxConsecutiveFailures"/>
    /// until the next heartbeat.
    /// </remarks>
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "OK.")]
    [SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "OK.")]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "OK.")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1304:Non-private readonly fields should begin with upper-case letter", Justification = "OK - protected fields are part of the public API.")]
    protected readonly IBackgroundServiceHealthService healthService;

    /// <summary>
    /// Work method run based on <see cref="IBackgroundScheduleServiceOptions.CronExpression" />.
    /// <br/><br/>
    /// Exceptions thrown here are passed to <see cref="OnExceptionAsync"/>, after which the unit of work is retried at the next occurrence.
    /// <br/><br/>
    /// Pass <paramref name="stoppingToken"/> on to every asynchronous call and return promptly when it's cancelled.
    /// The host waits for this method during shutdown, at most until its shutdown timeout
    /// (<c>HostOptions.ShutdownTimeout</c>, 30 seconds by default), and logs a warning if it doesn't return in time.
    /// </summary>
    /// <param name="stoppingToken">The stopping token.</param>
    /// <returns>The Task of the workload.</returns>
    public abstract Task DoWorkAsync(CancellationToken stoppingToken);

    /// <summary>
    /// Hook for handling an exception thrown by <see cref="DoWorkAsync"/>.
    /// <br/><br/>
    /// When this method returns normally, the failure is logged, and the unit of work is retried at the next occurrence
    /// of <see cref="IBackgroundScheduleServiceOptions.CronExpression"/>. This is the default behavior.
    /// <br/><br/>
    /// When this method throws (or returns a faulted task), the failure is fatal: the service stops, and the exception is
    /// passed to the host, which applies <c>HostOptions.BackgroundServiceExceptionBehavior</c>.
    /// The default, <c>BackgroundServiceExceptionBehavior.StopHost</c>, stops the host. Use this to escalate a failure that can't be recovered from.
    /// <br/><br/>
    /// Don't call <see cref="BackgroundService.StopAsync(CancellationToken)"/> here: it stops only this service and leaves the host running without it.
    /// To stop the whole application on purpose, use <see cref="IHostApplicationLifetime.StopApplication"/>.
    /// </summary>
    /// <param name="exception">The unhandled exception</param>
    /// <param name="stoppingToken">The stopping token</param>
    /// <returns>The Task of the exception handling</returns>
    protected virtual Task OnExceptionAsync(
        Exception exception,
        CancellationToken stoppingToken)
    {
        logger.LogBackgroundServiceUnhandledException(exception, ServiceName);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Logs a warning when the service hasn't stopped by the time the host gives up waiting for it,
    /// which usually means <see cref="DoWorkAsync"/> doesn't observe its stopping token.
    /// </remarks>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await base.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (ExecuteTask is { IsCompleted: false })
            {
                logger.LogBackgroundServiceStopTimedOut(ServiceName);
            }
        }
    }

    /// <inheritdoc />
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "OK - by design.")]
    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK - by design.")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!ServiceOptions.Enabled)
        {
            logger.LogBackgroundServiceDisabled(ServiceName);
            return;
        }

        logger.LogBackgroundServiceStarted(ServiceName, ServiceOptions.CronExpression);

        var heartbeat = new HeartbeatRunner(
            ServiceName,
            healthService,
            TimeProvider,
            logger,
            BackgroundServiceHeartbeat.ScheduleInterval,
            ServiceOptions);

        heartbeat.ReportAlive();

        using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var heartbeatTask = heartbeat.RunAsync(heartbeatCts.Token);

        try
        {
            var cronExpression = CronExpression.Parse(ServiceOptions.CronExpression);
            var nextOccurrence = cronExpression.GetNextOccurrence(TimeProvider.GetUtcNow().UtcDateTime);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (nextOccurrence.HasValue)
                {
                    var delay = nextOccurrence.Value - TimeProvider.GetUtcNow().UtcDateTime;

                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, TimeProvider, stoppingToken).ConfigureAwait(false);
                    }

                    try
                    {
                        heartbeat.MarkWorkStarted();
                        await DoWorkAsync(stoppingToken).ConfigureAwait(false);
                        heartbeat.MarkWorkSucceeded();
                    }
                    catch (Exception ex)
                    {
                        if (stoppingToken.IsCancellationRequested)
                        {
                            heartbeat.MarkWorkEnded();
                        }
                        else
                        {
                            heartbeat.MarkWorkFailed();
                        }

                        await OnExceptionAsync(ex, stoppingToken).ConfigureAwait(false);

                        stoppingToken.ThrowIfCancellationRequested();

                        logger.LogBackgroundServiceRetrying(
                            ServiceName,
                            ServiceOptions.CronExpression);
                    }

                    nextOccurrence = cronExpression.GetNextOccurrence(TimeProvider.GetUtcNow().UtcDateTime);
                }
                else
                {
                    logger.LogBackgroundServiceStopping(ServiceOptions.CronExpression, ServiceName);
                    break;
                }
            }
        }
        catch when (stoppingToken.IsCancellationRequested)
        {
            logger.LogBackgroundServiceCancelled(ServiceName);
        }
        catch (Exception ex)
        {
            // A fatal failure: log it with the service name, and let the host apply its BackgroundServiceExceptionBehavior.
            logger.LogBackgroundServiceUnhandledException(ex, ServiceName);
            throw;
        }
        finally
        {
            // Stop the heartbeat before reporting the final state, so a late heartbeat can't report a stopped service as running.
            await heartbeatCts.CancelAsync().ConfigureAwait(false);
            await heartbeatTask.ConfigureAwait(false);

            logger.LogBackgroundServiceStopped(ServiceName);
            healthService.SetRunningState(ServiceName, isRunning: false);
        }
    }
}