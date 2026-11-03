namespace Atc.Hosting.Tests.XUnitTestTypes;

public class MyScheduleWorkerService : BackgroundScheduleServiceBase<MyScheduleWorkerService>
{
    private int doWorkCallCount;

    public MyScheduleWorkerService(
        ILogger<MyScheduleWorkerService> logger,
        IOptions<MyScheduleServiceOptions> serviceOptions,
        IBackgroundServiceHealthService healthService,
        TimeProvider? timeProvider = null)
        : base(
            logger,
            serviceOptions.Value,
            healthService,
            timeProvider)
    {
    }

    public int DoWorkCallCount => Volatile.Read(ref doWorkCallCount);

    public override Task DoWorkAsync(CancellationToken stoppingToken)
    {
        Interlocked.Increment(ref doWorkCallCount);
        return Task.CompletedTask;
    }
}