namespace Atc.Hosting.TimeFile.Sample;

public class TimeFileWorker : BackgroundServiceBase<TimeFileWorker>
{
    private readonly TimeFileWorkerOptions workerOptions;
    private readonly ITimeService timeService;

    public TimeFileWorker(
        ILogger<TimeFileWorker> logger,
        IOptions<TimeFileWorkerOptions> workerOptions,
        ITimeService timeService)
        : base(
            logger,
            workerOptions.Value)
    {
        this.workerOptions = workerOptions.Value;
        this.timeService = timeService;
    }

    public override Task DoWorkAsync(
        CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(workerOptions.OutputDirectory);

        var time = timeService.GetDateTime();

        var outFile = Path.Combine(
            workerOptions.OutputDirectory,
            $"{time:yyyy-MM-dd--HHmmss}.txt");

        return File.WriteAllTextAsync(outFile, ServiceName, stoppingToken);
    }
}