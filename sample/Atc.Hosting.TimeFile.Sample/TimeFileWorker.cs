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

    public override async Task StartAsync(
        CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);
        healthService.SetRunningState(nameof(TimeFileWorker), isRunning: true);
    }

    public override async Task StopAsync(
        CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        healthService.SetRunningState(nameof(TimeFileWorker), isRunning: false);
    }

    public override async Task DoWorkAsync(
        CancellationToken stoppingToken)
    {
        var isServiceRunning = healthService.IsServiceRunning(nameof(TimeFileWorker));

        Directory.CreateDirectory(workerOptions.OutputDirectory);

        var time = timeProvider.UtcNow;

        var outFile = Path.Combine(
            workerOptions.OutputDirectory,
            $"{time:yyyy-MM-dd--HHmmss}-{isServiceRunning}.txt");

        await File.WriteAllTextAsync(outFile, $"{ServiceName}-{isServiceRunning}", stoppingToken);

        healthService.SetRunningState(nameof(TimeFileWorker), isRunning: true);
    }
}