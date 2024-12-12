namespace Atc.Hosting.Tests.XUnitTestTypes;

public class MyWorkerService : BackgroundServiceBase<MyWorkerService>
{
    private readonly Task longRunningTask;

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

    public MyWorkerService(
        ILogger<MyWorkerService> logger,
        IOptions<MyServiceOptions> serviceOptions,
        IBackgroundServiceHealthService healthService,
        Task longRunningTask)
        : base(
            logger,
            serviceOptions.Value,
            healthService)
    {
        this.longRunningTask = longRunningTask;
    }

    protected override Task OnExceptionAsync(
        Exception exception,
        CancellationToken stoppingToken)
        => exception is MyWorkerException
            ? throw exception
            : Task.CompletedTask;

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