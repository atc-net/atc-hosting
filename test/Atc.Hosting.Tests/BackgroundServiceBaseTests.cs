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
}