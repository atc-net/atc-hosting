namespace Atc.Hosting.Tests;

public sealed class BackgroundServiceBaseTests
{
    [Fact]
    public async Task StartAsync_Returns_CompletedTask_If_LongRunningTask_Is_Incomplete()
    {
        // Arrange
        var tcs = new TaskCompletionSource<object>();

        using var sut = new MyWorkerService(
            new NullLogger<MyWorkerService>(),
            new OptionsWrapper<MyServiceOptions>(new MyServiceOptions()),
            tcs.Task);

        // Act
        var actual = sut.StartAsync(CancellationToken.None);

        // Assert
        Assert.True(actual.IsCompleted);
        Assert.False(tcs.Task.IsCompleted);

        // Complete the task
        tcs.TrySetResult(null!);
        await actual;
    }

    [Fact]
    public async Task StartAsync_Returns_CompletedTask_If_Cancelled()
    {
        // Arrange
        var tcs = new TaskCompletionSource<object>();
        tcs.TrySetCanceled(TestContext.Current.CancellationToken);

        using var sut = new MyWorkerService(
            new NullLogger<MyWorkerService>(),
            new OptionsWrapper<MyServiceOptions>(new MyServiceOptions()),
            tcs.Task);

        // Act
        var actual = sut.StartAsync(CancellationToken.None);

        // Assert
        Assert.True(actual.IsCompleted);
        Assert.True(tcs.Task.IsCompleted);
        await actual;
    }

    [Fact]
    public async Task StopAsync_Without_StartAsync()
    {
        // Arrange
        var tcs = new TaskCompletionSource<object>();

        using var sut = new MyWorkerService(
            new NullLogger<MyWorkerService>(),
            new OptionsWrapper<MyServiceOptions>(new MyServiceOptions()),
            tcs.Task);

        // Act
        await sut.StopAsync(CancellationToken.None);

        // Assert
        Assert.False(tcs.Task.IsCompleted);
    }

    [Fact]
    public async Task StopAsync_Stops_BackgroundService()
    {
        // Arrange
        var tcs = new TaskCompletionSource<object>();

        using var sut = new MyWorkerService(
            new NullLogger<MyWorkerService>(),
            new OptionsWrapper<MyServiceOptions>(new MyServiceOptions()),
            tcs.Task);

        // Assert 1
        Assert.Null(sut.ExecuteTask);

        // Act & Assert 2
        await sut.StartAsync(CancellationToken.None);
        Assert.NotNull(sut.ExecuteTask);
        Assert.False(sut.ExecuteTask.IsCompleted);

        // Act & Assert 3
        await sut.StopAsync(CancellationToken.None);
        Assert.NotNull(sut.ExecuteTask);
        Assert.True(sut.ExecuteTask.IsCompleted);
    }

    [Fact]
    public async Task Logging_Start_Retry_Cancel_Stop()
    {
        // Arrange
        var exception = new Exception("Test exception");
        var logger = Substitute.For<MockLogger<MyWorkerService>>();
        var options = new MyServiceOptions();

        logger
            .IsEnabled(default)
            .ReturnsForAnyArgs(true);

        var tcs = new TaskCompletionSource();
        tcs.SetException(exception);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(1500);

        using var sut = new MyWorkerService(
            logger,
            new OptionsWrapper<MyServiceOptions>(options),
            tcs.Task);

        var serviceName = sut.ServiceName;

        // Act
        await sut.StartAsync(cts.Token);

        await Task.Delay(2000, TestContext.Current.CancellationToken);

        // Assert
        Received.InOrder(
            () =>
            {
                logger
                    .Log(
                        LogLevel.Information,
                        $"Started worker {serviceName}. Worker will run with {options.RepeatIntervalSeconds} seconds interval");

                logger
                    .Log(
                        LogLevel.Information,
                        $"Worker {serviceName} will retry after {options.RepeatIntervalSeconds} seconds");

                logger
                    .Log(
                        LogLevel.Warning,
                        $"Cancellation invoked for worker {serviceName}");

                logger
                    .Log(
                        LogLevel.Information,
                        $"Stopped worker {serviceName}");
            });
    }

    [Fact]
    public async Task Logging_Start_Exception_Stop()
    {
        // Arrange
        var exception = new MyWorkerException("Test exception");
        var logger = Substitute.For<MockLogger<MyWorkerService>>();
        var options = new MyServiceOptions();

        logger
            .IsEnabled(default)
            .ReturnsForAnyArgs(true);

        var tcs = new TaskCompletionSource();
        tcs.SetException(exception);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(1500);

        using var sut = new MyWorkerService(
            logger,
            new OptionsWrapper<MyServiceOptions>(options),
            tcs.Task);

        var serviceName = sut.ServiceName;

        // Act
        await sut.StartAsync(cts.Token);

        await Task.Delay(2000, TestContext.Current.CancellationToken);

        // Assert
        Received.InOrder(
            () =>
            {
                logger
                    .Log(
                        LogLevel.Information,
                        $"Started worker {serviceName}. Worker will run with {options.RepeatIntervalSeconds} seconds interval");

                logger
                    .Log(
                        LogLevel.Error,
                        $"Unhandled exception occurred in worker {serviceName}",
                        exception);

                logger
                    .Log(
                        LogLevel.Information,
                        $"Stopped worker {serviceName}");
            });

        // A fatal failure is rethrown, so the host can apply its BackgroundServiceExceptionBehavior.
        Assert.True(sut.ExecuteTask!.IsFaulted);
        Assert.Same(exception, sut.ExecuteTask.Exception!.InnerException);
    }

    [Fact]
    public async Task HealthService_Start_Retry_Cancel_Stop()
    {
        // Arrange
        var exception = new Exception("Test exception");
        var logger = Substitute.For<MockLogger<MyWorkerService>>();
        var options = new MyServiceOptions();
        var healthService = Substitute.For<IBackgroundServiceHealthService>();

        logger
            .IsEnabled(default)
            .ReturnsForAnyArgs(true);

        var tcs = new TaskCompletionSource();
        tcs.SetException(exception);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(2500);

        using var sut = new MyWorkerService(
            logger,
            new OptionsWrapper<MyServiceOptions>(options),
            healthService,
            tcs.Task);

        var serviceName = sut.ServiceName;

        // Act
        await sut.StartAsync(cts.Token);

        await Task.Delay(3000, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(sut.ExecuteTask!.IsCompletedSuccessfully);
        healthService
            .Received()
            .SetRunningState(serviceName, true);

        AssertLastRunningState(healthService, serviceName, expected: false);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_DoesNotDoWorkOrReportRunning()
    {
        // Arrange
        var logger = Substitute.For<MockLogger<MyWorkerService>>();
        var options = new MyServiceOptions { Enabled = false, StartupDelaySeconds = 0 };
        var healthService = Substitute.For<IBackgroundServiceHealthService>();

        logger
            .IsEnabled(default)
            .ReturnsForAnyArgs(true);

        using var sut = new MyWorkerService(
            logger,
            new OptionsWrapper<MyServiceOptions>(options),
            healthService,
            Task.CompletedTask);

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
    public async Task HealthService_Start_Exception_Stop()
    {
        // Arrange
        var exception = new MyWorkerException("Test exception");
        var logger = Substitute.For<MockLogger<MyWorkerService>>();
        var options = new MyServiceOptions();
        var healthService = Substitute.For<IBackgroundServiceHealthService>();

        logger
            .IsEnabled(default)
            .ReturnsForAnyArgs(true);

        var tcs = new TaskCompletionSource();
        tcs.SetException(exception);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(1500);

        using var sut = new MyWorkerService(
            logger,
            new OptionsWrapper<MyServiceOptions>(options),
            healthService,
            tcs.Task);

        var serviceName = sut.ServiceName;

        // Act
        await sut.StartAsync(cts.Token);

        await Task.Delay(2000, TestContext.Current.CancellationToken);

        // Assert
        var executeException = await Assert.ThrowsAsync<MyWorkerException>(() => sut.ExecuteTask!);
        Assert.Same(exception, executeException);
        healthService
            .Received()
            .SetRunningState(serviceName, true);

        AssertLastRunningState(healthService, serviceName, expected: false);
    }

    [Fact]
    public async Task Heartbeat_LongRun_StaysRunning_WithStalenessEqualToRepeatInterval()
    {
        // Arrange - the README's manual pattern: staleness = RepeatIntervalSeconds
        var timeProvider = new FakeTimeProvider();
        var healthService = new BackgroundServiceHealthService(timeProvider);
        var options = new MyServiceOptions { StartupDelaySeconds = 0, RepeatIntervalSeconds = 2 };
        var neverCompletes = new TaskCompletionSource();

        using var sut = new MyWorkerService(
            NullLogger<MyWorkerService>.Instance,
            new OptionsWrapper<MyServiceOptions>(options),
            healthService,
            timeProvider,
            neverCompletes.Task);

        healthService.SetMaxStalenessInSeconds(sut.ServiceName, options.RepeatIntervalSeconds);

        // Act & Assert - a single run lasting 5x the repeat interval stays healthy
        await sut.StartAsync(CancellationToken.None);
        await TestWait.UntilAsync(() => sut.DoWorkCallCount == 1);

        for (var i = 0; i < 10; i++)
        {
            timeProvider.Advance(TimeSpan.FromSeconds(1));
            await TestWait.UntilAsync(() => healthService.IsServiceRunning(sut.ServiceName));
        }

        await sut.StopAsync(CancellationToken.None);
        Assert.False(healthService.IsServiceRunning(sut.ServiceName));
    }

    [Fact]
    public async Task Heartbeat_AfterStop_DoesNotReportRunningAgain()
    {
        // Arrange - real time, so the heartbeat ticks every second
        var healthService = new BackgroundServiceHealthService(TimeProvider.System);
        var options = new MyServiceOptions { StartupDelaySeconds = 0, RepeatIntervalSeconds = 2 };

        using var sut = new MyWorkerService(
            NullLogger<MyWorkerService>.Instance,
            new OptionsWrapper<MyServiceOptions>(options),
            healthService,
            new TaskCompletionSource().Task);

        // Act
        await sut.StartAsync(CancellationToken.None);
        await Task.Delay(1500, TestContext.Current.CancellationToken);
        await sut.StopAsync(CancellationToken.None);
        await Task.Delay(1500, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(healthService.IsServiceRunning(sut.ServiceName));
    }

    [Fact]
    public async Task MaxConsecutiveFailures_FailingWorker_IsReportedNotRunning()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var logger = Substitute.For<MockLogger<MyWorkerService>>();
        var healthService = new BackgroundServiceHealthService(timeProvider);
        var options = new MyServiceOptions { StartupDelaySeconds = 0, RepeatIntervalSeconds = 1, MaxConsecutiveFailures = 2 };

        logger
            .IsEnabled(default)
            .ReturnsForAnyArgs(true);

        using var sut = new MyWorkerService(
            logger,
            new OptionsWrapper<MyServiceOptions>(options),
            healthService,
            timeProvider,
            Task.FromException(new InvalidOperationException("Always fails")));

        // Act
        await sut.StartAsync(CancellationToken.None);
        await TestWait.AdvanceUntilAsync(
            timeProvider,
            TimeSpan.FromSeconds(1),
            () => sut.DoWorkCallCount >= 2 && !healthService.IsServiceRunning(sut.ServiceName));

        await sut.StopAsync(CancellationToken.None);

        // Assert
        logger
            .Received(1)
            .Log(
                LogLevel.Warning,
                $"Worker {sut.ServiceName} has failed 2 times in a row and is reported as not running until a run succeeds");
    }

    [Fact]
    public async Task StopAsync_WorkerIgnoresStoppingToken_LogsStopTimeoutWarning()
    {
        // Arrange
        var logger = Substitute.For<MockLogger<MyTokenIgnoringWorkerService>>();
        var options = new MyServiceOptions { StartupDelaySeconds = 0 };

        logger
            .IsEnabled(default)
            .ReturnsForAnyArgs(true);

        using var sut = new MyTokenIgnoringWorkerService(
            logger,
            new OptionsWrapper<MyServiceOptions>(options),
            Substitute.For<IBackgroundServiceHealthService>());

        await sut.StartAsync(CancellationToken.None);
        await sut.WorkStarted.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Act - an already-cancelled token means the host's shutdown timeout has expired
        await sut.StopAsync(new CancellationToken(canceled: true));

        // Assert
        logger
            .Received(1)
            .Log(
                LogLevel.Warning,
                $"Worker {sut.ServiceName} did not stop within the shutdown timeout. Make sure DoWorkAsync observes the stopping token");

        sut.Release();
        await sut.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task StopAsync_WorkerObservesStoppingToken_DoesNotLogStopTimeoutWarning()
    {
        // Arrange
        var logger = Substitute.For<MockLogger<MyWorkerService>>();
        var options = new MyServiceOptions { StartupDelaySeconds = 0 };

        logger
            .IsEnabled(default)
            .ReturnsForAnyArgs(true);

        using var sut = new MyWorkerService(
            logger,
            new OptionsWrapper<MyServiceOptions>(options),
            new TaskCompletionSource().Task);

        await sut.StartAsync(CancellationToken.None);
        await TestWait.UntilAsync(() => sut.DoWorkCallCount == 1);

        // Act
        await sut.StopAsync(CancellationToken.None);

        // Assert
        logger
            .DidNotReceive()
            .Log(
                LogLevel.Warning,
                $"Worker {sut.ServiceName} did not stop within the shutdown timeout. Make sure DoWorkAsync observes the stopping token");
    }

    private static void AssertLastRunningState(
        IBackgroundServiceHealthService healthService,
        string serviceName,
        bool expected)
    {
        var lastCall = healthService
            .ReceivedCalls()
            .Last(c => c.GetMethodInfo().Name == nameof(IBackgroundServiceHealthService.SetRunningState));

        Assert.Equal([serviceName, expected], lastCall.GetArguments());
    }
}