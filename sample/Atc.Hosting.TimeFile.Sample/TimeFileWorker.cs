namespace Atc.Hosting.TimeFile.Sample;

public class TimeFileWorker : BackgroundServiceBase<TimeFileWorker>
{
    private readonly ITimeProvider timeProvider;

    private readonly TimeFileWorkerOptions workerOptions;

    public TimeFileWorker(
        ILogger<TimeFileWorker> logger,
        IBackgroundServiceHealthService healthService,
        ITimeProvider timeProvider,
        IOptions<TimeFileWorkerOptions> workerOptions)
        : base(
            logger,
            workerOptions.Value,
            healthService)
    {
        this.timeProvider = timeProvider;
        this.workerOptions = workerOptions.Value;
    }

    public override Task StartAsync(
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Started");
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopped");
        return base.StopAsync(cancellationToken);
    }

    public override Task DoWorkAsync(
        CancellationToken stoppingToken)
    {
        var isServiceRunning = healthService.IsServiceRunning(nameof(TimeFileWorker));

        Directory.CreateDirectory(workerOptions.OutputDirectory);

        var time = timeProvider.UtcNow;

        var outFile = Path.Combine(
            workerOptions.OutputDirectory,
            $"{nameof(TimeFileWorker)}.txt");

        return File.AppendAllLinesAsync(
            outFile,
            [$"{time:yyyy-MM-dd HH:mm:ss} - {ServiceName} - IsRunning={isServiceRunning}"],
            stoppingToken);
    }

    protected override Task OnExceptionAsync(
        Exception exception,
        CancellationToken stoppingToken)
    {
        if (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogCritical(exception, "Could not write file!");
            return StopAsync(stoppingToken);
        }

        return base.OnExceptionAsync(exception, stoppingToken);
    }
}