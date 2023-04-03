namespace Atc.Hosting.Tests.XUnitTestTypes;

public class MyWorkerService : BackgroundServiceBase<MyWorkerService>
{
    private readonly Task longRunningTask;

    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "OK.")]
    public MyWorkerService(
        ILogger<MyWorkerService> logger,
        IOptions<MyServiceOptions> serviceOptions,
        Task longRunningTask)
        : base(
            logger,
            serviceOptions.Value)
    {
        this.longRunningTask = longRunningTask;
    }

    public override Task DoWorkAsync(
        CancellationToken stoppingToken)
        => ExecuteCore(stoppingToken);

    private async Task ExecuteCore(
        CancellationToken stoppingToken)
    {
        var currentTask = await Task.WhenAny(
            longRunningTask,
            Task.Delay(Timeout.Infinite, stoppingToken));

        await currentTask;
    }
}