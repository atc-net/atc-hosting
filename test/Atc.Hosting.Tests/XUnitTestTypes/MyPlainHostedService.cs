namespace Atc.Hosting.Tests.XUnitTestTypes;

public sealed class MyPlainHostedService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}