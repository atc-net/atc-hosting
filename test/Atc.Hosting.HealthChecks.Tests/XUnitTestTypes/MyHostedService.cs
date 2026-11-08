namespace Atc.Hosting.HealthChecks.Tests.XUnitTestTypes;

public sealed class MyHostedService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}