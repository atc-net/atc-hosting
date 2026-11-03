namespace Atc.Hosting.Internal;

/// <summary>
/// Reports a background service as running at a fixed interval, and owns every "running" report for the service,
/// so a service that has exceeded <see cref="IBackgroundServiceBaseOptions.MaxWorkDurationSeconds"/> or
/// <see cref="IBackgroundServiceBaseOptions.MaxConsecutiveFailures"/> can't be turned healthy again by the worker loop.
/// </summary>
internal sealed class HeartbeatRunner
{
    private readonly Lock syncLock = new();
    private readonly string serviceName;
    private readonly IBackgroundServiceHealthService healthService;
    private readonly TimeProvider timeProvider;
    private readonly ILogger logger;
    private readonly TimeSpan interval;
    private readonly ushort maxWorkDurationSeconds;
    private readonly ushort maxConsecutiveFailures;

    private DateTimeOffset? workStartedAt;
    private bool isOverdue;
    private int consecutiveFailures;
    private bool hasTooManyFailures;

    public HeartbeatRunner(
        string serviceName,
        IBackgroundServiceHealthService healthService,
        TimeProvider timeProvider,
        ILogger logger,
        TimeSpan interval,
        IBackgroundServiceBaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.serviceName = serviceName;
        this.healthService = healthService;
        this.timeProvider = timeProvider;
        this.logger = logger;
        this.interval = interval;
        maxWorkDurationSeconds = options.MaxWorkDurationSeconds ?? 0;
        maxConsecutiveFailures = options.MaxConsecutiveFailures ?? 0;
    }

    /// <summary>
    /// Ticks every interval until <paramref name="cancellationToken"/> is cancelled. Cancellation completes the task without throwing.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the heartbeat has stopped.</returns>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(interval, timeProvider);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                Tick();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Stopping the heartbeat is the expected way to end it.
        }
    }

    /// <summary>
    /// Handles one heartbeat: detects a run that has exceeded the max work duration, then reports the current state.
    /// </summary>
    /// <remarks>
    /// While the service is overdue or has failed too many times in a row, every tick reports it as not running again.
    /// That corrects a "running" report made outside this class (e.g. a worker calling SetRunningState itself) within one interval.
    /// </remarks>
    public void Tick()
    {
        lock (syncLock)
        {
            if (!isOverdue &&
                maxWorkDurationSeconds > 0 &&
                workStartedAt.HasValue &&
                timeProvider.GetUtcNow() - workStartedAt.Value > TimeSpan.FromSeconds(maxWorkDurationSeconds))
            {
                isOverdue = true;
                logger.LogBackgroundServiceWorkExceededMaxDuration(serviceName, maxWorkDurationSeconds);
            }

            ReportStateLocked();
        }
    }

    /// <summary>
    /// Reports the current state: running, unless the service is overdue or has failed too many times in a row.
    /// </summary>
    public void ReportAlive()
    {
        lock (syncLock)
        {
            ReportStateLocked();
        }
    }

    /// <summary>
    /// Records that a unit of work has started.
    /// </summary>
    public void MarkWorkStarted()
    {
        lock (syncLock)
        {
            workStartedAt = timeProvider.GetUtcNow();
            isOverdue = false;
            ReportStateLocked();
        }
    }

    /// <summary>
    /// Records that a unit of work has succeeded, which resets the consecutive failures.
    /// </summary>
    public void MarkWorkSucceeded()
    {
        lock (syncLock)
        {
            EndWorkLocked();
            consecutiveFailures = 0;
            hasTooManyFailures = false;
            ReportStateLocked();
        }
    }

    /// <summary>
    /// Records that a unit of work has failed and will be retried.
    /// </summary>
    public void MarkWorkFailed()
    {
        lock (syncLock)
        {
            EndWorkLocked();
            consecutiveFailures++;

            if (!hasTooManyFailures &&
                maxConsecutiveFailures > 0 &&
                consecutiveFailures >= maxConsecutiveFailures)
            {
                hasTooManyFailures = true;
                logger.LogBackgroundServiceConsecutiveFailures(serviceName, consecutiveFailures);
            }

            ReportStateLocked();
        }
    }

    /// <summary>
    /// Records that a unit of work has ended without counting as a success or a failure, e.g. because the service is stopping.
    /// </summary>
    public void MarkWorkEnded()
    {
        lock (syncLock)
        {
            EndWorkLocked();
        }
    }

    private void EndWorkLocked()
    {
        workStartedAt = null;
        isOverdue = false;
    }

    private void ReportStateLocked()
        => healthService.SetRunningState(
            serviceName,
            isRunning: !isOverdue && !hasTooManyFailures);
}