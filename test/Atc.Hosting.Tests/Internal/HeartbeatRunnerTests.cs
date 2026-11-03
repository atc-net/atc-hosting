namespace Atc.Hosting.Tests.Internal;

public sealed class HeartbeatRunnerTests
{
    private const string ServiceName = "MyService";

    private readonly FakeTimeProvider timeProvider = new();
    private readonly MockLogger<HeartbeatRunnerTests> logger = Substitute.For<MockLogger<HeartbeatRunnerTests>>();
    private readonly BackgroundServiceHealthService healthService;

    public HeartbeatRunnerTests()
    {
        logger
            .IsEnabled(default)
            .ReturnsForAnyArgs(true);

        // No staleness is configured, so these tests show the limits work without it.
        healthService = new BackgroundServiceHealthService(timeProvider);
    }

    [Fact]
    public void Tick_ReportsRunning()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Tick();

        // Assert
        Assert.True(healthService.IsServiceRunning(ServiceName));
    }

    [Fact]
    public void Tick_LongRunWithoutMaxWorkDuration_StaysRunning()
    {
        // Arrange
        var sut = CreateSut();
        sut.MarkWorkStarted();

        // Act
        timeProvider.Advance(TimeSpan.FromHours(1));
        sut.Tick();

        // Assert
        Assert.True(healthService.IsServiceRunning(ServiceName));
    }

    [Fact]
    public void Tick_RunExceedsMaxWorkDuration_ReportsNotRunningUntilTheRunEnds()
    {
        // Arrange
        var sut = CreateSut(maxWorkDurationSeconds: 5);
        sut.MarkWorkStarted();

        // Act & Assert - at the limit, still running
        timeProvider.Advance(TimeSpan.FromSeconds(5));
        sut.Tick();
        Assert.True(healthService.IsServiceRunning(ServiceName));

        // Act & Assert - past the limit, not running, and later ticks don't report it running again
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        sut.Tick();
        Assert.False(healthService.IsServiceRunning(ServiceName));

        sut.Tick();
        sut.ReportAlive();
        Assert.False(healthService.IsServiceRunning(ServiceName));

        logger
            .Received(1)
            .Log(
                LogLevel.Warning,
                $"Worker {ServiceName} has been running for more than 5 seconds and is reported as not running until the run completes");

        // Act & Assert - the run completes
        sut.MarkWorkSucceeded();
        Assert.True(healthService.IsServiceRunning(ServiceName));
    }

    [Fact]
    public void MarkWorkFailed_ReachesMaxConsecutiveFailures_ReportsNotRunningUntilARunSucceeds()
    {
        // Arrange
        var sut = CreateSut(maxConsecutiveFailures: 3);

        // Act & Assert - failures 1 and 2
        for (var i = 0; i < 2; i++)
        {
            sut.MarkWorkStarted();
            sut.MarkWorkFailed();
            Assert.True(healthService.IsServiceRunning(ServiceName));
        }

        // Act & Assert - failure 3 reaches the limit
        sut.MarkWorkStarted();
        sut.MarkWorkFailed();
        Assert.False(healthService.IsServiceRunning(ServiceName));

        // Act & Assert - retries and ticks don't report it running again
        sut.MarkWorkStarted();
        Assert.False(healthService.IsServiceRunning(ServiceName));
        sut.Tick();
        Assert.False(healthService.IsServiceRunning(ServiceName));
        sut.MarkWorkFailed();
        Assert.False(healthService.IsServiceRunning(ServiceName));

        logger
            .Received(1)
            .Log(
                LogLevel.Warning,
                $"Worker {ServiceName} has failed 3 times in a row and is reported as not running until a run succeeds");

        // Act & Assert - a successful run resets it
        sut.MarkWorkStarted();
        sut.MarkWorkSucceeded();
        Assert.True(healthService.IsServiceRunning(ServiceName));
    }

    [Fact]
    public void Tick_WhileNotRunning_CorrectsAManualRunningReport()
    {
        // Arrange
        var sut = CreateSut(maxConsecutiveFailures: 1);

        sut.MarkWorkStarted();
        sut.MarkWorkFailed();

        // Act - a worker reports itself as running, bypassing the runner
        healthService.SetRunningState(ServiceName, isRunning: true);
        sut.Tick();

        // Assert
        Assert.False(healthService.IsServiceRunning(ServiceName));
    }

    [Fact]
    public void MarkWorkSucceeded_ResetsTheConsecutiveFailureCount()
    {
        // Arrange
        var sut = CreateSut(maxConsecutiveFailures: 2);

        // Act
        sut.MarkWorkFailed();
        sut.MarkWorkSucceeded();
        sut.MarkWorkFailed();

        // Assert
        Assert.True(healthService.IsServiceRunning(ServiceName));
    }

    [Theory]
    [InlineData(null)]
    [InlineData((ushort)0)]
    public void MarkWorkFailed_WithoutMaxConsecutiveFailures_StaysRunning(
        ushort? maxConsecutiveFailures)
    {
        // Arrange
        var sut = CreateSut(maxConsecutiveFailures: maxConsecutiveFailures);

        // Act
        for (var i = 0; i < 100; i++)
        {
            sut.MarkWorkStarted();
            sut.MarkWorkFailed();
        }

        // Assert
        Assert.True(healthService.IsServiceRunning(ServiceName));
    }

    [Fact]
    public void MarkWorkEnded_DoesNotCountAsAFailure()
    {
        // Arrange
        var sut = CreateSut(maxConsecutiveFailures: 1);

        // Act
        sut.MarkWorkStarted();
        sut.MarkWorkEnded();
        sut.Tick();

        // Assert
        Assert.True(healthService.IsServiceRunning(ServiceName));
    }

    [Fact]
    public async Task RunAsync_TicksEveryInterval_AndCompletesOnCancellation()
    {
        // Arrange
        var substituteHealthService = Substitute.For<IBackgroundServiceHealthService>();
        var sut = new HeartbeatRunner(
            ServiceName,
            substituteHealthService,
            timeProvider,
            logger,
            TimeSpan.FromSeconds(1),
            new DefaultBackgroundServiceOptions());

        using var cts = new CancellationTokenSource();

        // Act
        var runTask = sut.RunAsync(cts.Token);
        await TestWait.AdvanceUntilAsync(
            timeProvider,
            TimeSpan.FromSeconds(1),
            () => substituteHealthService.ReceivedCalls().Any());

        await cts.CancelAsync();
        await runTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(runTask.IsCompletedSuccessfully);
        substituteHealthService
            .Received()
            .SetRunningState(ServiceName, true);
    }

    private HeartbeatRunner CreateSut(
        ushort? maxWorkDurationSeconds = null,
        ushort? maxConsecutiveFailures = null)
        => new(
            ServiceName,
            healthService,
            timeProvider,
            logger,
            TimeSpan.FromSeconds(1),
            new DefaultBackgroundServiceOptions
            {
                MaxWorkDurationSeconds = maxWorkDurationSeconds,
                MaxConsecutiveFailures = maxConsecutiveFailures,
            });
}