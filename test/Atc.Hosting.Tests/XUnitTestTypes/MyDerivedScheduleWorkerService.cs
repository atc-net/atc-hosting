namespace Atc.Hosting.Tests.XUnitTestTypes;

public sealed class MyDerivedScheduleWorkerService : MyScheduleWorkerService
{
    public MyDerivedScheduleWorkerService(
        ILogger<MyScheduleWorkerService> logger,
        IOptions<MyScheduleServiceOptions> serviceOptions,
        IBackgroundServiceHealthService healthService,
        TimeProvider? timeProvider = null)
        : base(
            logger,
            serviceOptions,
            healthService,
            timeProvider)
    {
    }
}