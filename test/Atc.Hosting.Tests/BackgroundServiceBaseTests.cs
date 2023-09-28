namespace Atc.Hosting.Tests;

public class BackgroundServiceBaseTests
{
    [Fact]
    public void StartAsync_Returns_CompletedTask_If_LongRunningTask_Is_Incomplete()
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
    }

    [Fact]
    public void StartAsync_Returns_CompletedTask_If_Cancelled()
    {
        // Arrange
        var tcs = new TaskCompletionSource<object>();
        tcs.TrySetCanceled();

        using var sut = new MyWorkerService(
            new NullLogger<MyWorkerService>(),
            new OptionsWrapper<MyServiceOptions>(new MyServiceOptions()),
            tcs.Task);

        // Act
        var actual = sut.StartAsync(CancellationToken.None);

        // Assert
        Assert.True(actual.IsCompleted);
        Assert.True(tcs.Task.IsCompleted);
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
    [SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "OK - for testing.")]
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

        // Act
        await sut.StartAsync(cts.Token);

        await Task.Delay(2000);

        // Assert
        Received.InOrder(
            () =>
            {
                logger
                    .Log(
                        LogLevel.Information,
                        $"Started worker {sut.ServiceName}. Worker will run with {options.RepeatIntervalSeconds} seconds interval");

                logger
                    .Log(
                        LogLevel.Warning,
                        $"Unhandled exception occurred in worker {sut.ServiceName}. Worker will retry after {options.RepeatIntervalSeconds} seconds",
                        exception);

                logger
                    .Log(
                        LogLevel.Warning,
                        $"Cancellation invoked for worker {sut.ServiceName}");

                logger
                    .Log(
                        LogLevel.Information,
                        $"Stopped worker {sut.ServiceName}");
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

        // Act
        await sut.StartAsync(cts.Token);

        await Task.Delay(2000);

        // Assert
        Received.InOrder(
            () =>
            {
                logger
                    .Log(
                        LogLevel.Information,
                        $"Started worker {sut.ServiceName}. Worker will run with {options.RepeatIntervalSeconds} seconds interval");

                logger
                    .Log(
                        LogLevel.Error,
                        $"Unhandled exception occurred in worker {sut.ServiceName}",
                        exception);

                logger
                    .Log(
                        LogLevel.Information,
                        $"Stopped worker {sut.ServiceName}");
            });
    }

    [Fact]
    [SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "OK - for testing.")]
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

        // Act
        await sut.StartAsync(cts.Token);

        await Task.Delay(3000);

        // Assert
        Received.InOrder(
            () =>
            {
                healthService.SetRunningState(sut.ServiceName, true);
                healthService.SetRunningState(sut.ServiceName, false);
            });
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

        // Act
        await sut.StartAsync(cts.Token);

        await Task.Delay(2000);

        // Assert
        Received.InOrder(
            () =>
            {
                healthService.SetRunningState(sut.ServiceName, true);
                healthService.SetRunningState(sut.ServiceName, false);
            });
    }
}