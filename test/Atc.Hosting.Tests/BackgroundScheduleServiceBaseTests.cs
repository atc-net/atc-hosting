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

    [Fact]
    public async Task Heartbeat_WaitingForTheNextOccurrence_StaysRunning()
    {
        // Arrange - an hourly schedule, started just after the hour, with the staleness the registration applies
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 30, TimeSpan.Zero));
        var healthService = new BackgroundServiceHealthService(timeProvider);
        var options = new MyScheduleServiceOptions { CronExpression = "0 * * * *" };

        using var sut = new MyScheduleWorkerService(
            NullLogger<MyScheduleWorkerService>.Instance,
            new OptionsWrapper<MyScheduleServiceOptions>(options),
            healthService,
            timeProvider);

        healthService.SetMaxStalenessInSeconds(
            sut.ServiceName,
            BackgroundServiceHeartbeat.GetMaxStalenessInSeconds(BackgroundServiceHeartbeat.ScheduleInterval));

        // Act & Assert - two minutes of waiting
        await sut.StartAsync(CancellationToken.None);
        await TestWait.UntilAsync(() => healthService.IsServiceRunning(sut.ServiceName));

        for (var i = 0; i < 12; i++)
        {
            timeProvider.Advance(TimeSpan.FromSeconds(10));
            await TestWait.UntilAsync(() => healthService.IsServiceRunning(sut.ServiceName));
        }

        Assert.Equal(0, sut.DoWorkCallCount);

        await sut.StopAsync(CancellationToken.None);
        Assert.False(healthService.IsServiceRunning(sut.ServiceName));
    }

    [Fact]
    public async Task ExecuteAsync_InvalidCronExpression_IsFatal()
    {
        // Arrange
        var options = new MyScheduleServiceOptions { CronExpression = "not a cron expression" };
        var healthService = Substitute.For<IBackgroundServiceHealthService>();

        using var sut = new MyScheduleWorkerService(
            NullLogger<MyScheduleWorkerService>.Instance,
            new OptionsWrapper<MyScheduleServiceOptions>(options),
            healthService);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert - the failure reaches the host, and the final state is "not running"
        await Assert.ThrowsAsync<Cronos.CronFormatException>(() => sut.ExecuteTask!);
        healthService
            .Received()
            .SetRunningState(sut.ServiceName, false);
    }
}