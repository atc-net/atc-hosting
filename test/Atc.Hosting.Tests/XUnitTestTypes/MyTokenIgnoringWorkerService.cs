namespace Atc.Hosting.Tests.XUnitTestTypes;

/// <summary>
/// A worker whose <see cref="DoWorkAsync"/> ignores its stopping token and only returns when <see cref="Release"/> is called.
/// </summary>
public class MyTokenIgnoringWorkerService : BackgroundServiceBase<MyTokenIgnoringWorkerService>
{
    private readonly TaskCompletionSource workStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public MyTokenIgnoringWorkerService(
        ILogger<MyTokenIgnoringWorkerService> logger,
        IOptions<MyServiceOptions> serviceOptions,
        IBackgroundServiceHealthService healthService)
        : base(
            logger,
            serviceOptions.Value,
            healthService)
    {
    }

    public Task WorkStarted => workStarted.Task;

    public void Release()
        => release.TrySetResult();

    public override Task DoWorkAsync(CancellationToken stoppingToken)
    {
        workStarted.TrySetResult();
        return release.Task;
    }
}