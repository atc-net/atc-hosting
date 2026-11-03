namespace Atc.Hosting.Tests;

public sealed class BackgroundScheduleServiceBaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenDisabled_DoesNotDoWorkOrReportRunning()
    {
        // Arrange
        var logger = Substitute.For<MockLogger<MyScheduleWorkerService>>();
        var options = new MyScheduleServiceOptions { Enabled = false };
        var healthService = Substitute.For<IBackgroundServiceHealthService>();

        logger
            .IsEnabled(default)
            .ReturnsForAnyArgs(true);

        using var sut = new MyScheduleWorkerService(
            logger,
            new OptionsWrapper<MyScheduleServiceOptions>(options),
            healthService);

        // Act
        await sut.StartAsync(CancellationToken.None);
        await sut.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await sut.StopAsync(CancellationToken.None);

        // Assert
        Assert.True(sut.ExecuteTask.IsCompletedSuccessfully);
        Assert.Equal(0, sut.DoWorkCallCount);
        healthService
            .DidNotReceiveWithAnyArgs()
            .SetRunningState(default!, default);
        logger
            .Received(1)
            .Log(
                LogLevel.Information,
                $"Background service '{sut.ServiceName}' is disabled and will not run");
    }
}