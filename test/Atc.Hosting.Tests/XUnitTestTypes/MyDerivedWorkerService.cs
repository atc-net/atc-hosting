namespace Atc.Hosting.Tests.XUnitTestTypes;

public sealed class MyDerivedWorkerService : MyWorkerService
{
    public MyDerivedWorkerService(
        ILogger<MyWorkerService> logger,
        IOptions<MyServiceOptions> serviceOptions,
        Task longRunningTask)
        : base(
            logger,
            serviceOptions,
            longRunningTask)
    {
    }
}