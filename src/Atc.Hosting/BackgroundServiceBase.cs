namespace Atc.Hosting;

/// <summary>
/// BackgroundService provides consistent logging (including a logger enriched with the type of the service).
/// The <see cref="DoWorkAsync"/> method is called indefinitely, so long as it is supposed to.
/// </summary>
/// <typeparam name="T">The service type.</typeparam>
public abstract partial class BackgroundServiceBase<T> : BackgroundService
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
    /// Work method run based on <see cref="IBackgroundServiceOptions.RepeatIntervalSeconds" />.
    /// Exceptions thrown here are turned into alerts / logs with severity of <see cref="LogLevel.Warning" />.
    /// </summary>
    /// <param name="stoppingToken">The stopping token.</param>
    /// <returns>The Task of the workload.</returns>
    public abstract Task DoWorkAsync(
        CancellationToken stoppingToken);

    /// <inheritdoc />
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "OK - by design.")]
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        // Awaiting Task.Yield() transitions to asynchronous operation immediately.
        // This allows startup to continue without waiting.
        await Task.Yield();

        LogBackgroundServiceStarted(ServiceName, ServiceOptions.RepeatIntervalSeconds);

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
                    LogBackgroundServiceRetrying(
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
            LogBackgroundServiceCancelled(ServiceName);
        }
        catch (Exception ex)
        {
            LogBackgroundServiceUnhandledException(ServiceName, ex);
        }
        finally
        {
            LogBackgroundServiceStopped(ServiceName, stoppingToken.IsCancellationRequested);
        }
    }
}