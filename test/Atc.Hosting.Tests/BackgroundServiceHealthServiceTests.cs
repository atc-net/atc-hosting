namespace Atc.Hosting.Tests;

public sealed class BackgroundServiceHealthServiceTests
{
    private const ushort GracePeriodInSeconds = 1;

    [Theory]
    [InlineAutoNSubstituteData("TimeFileWorker")]
    public void SetMaxStalenessInSecondsTest(
        string serviceName,
        [Frozen] BackgroundServiceHealthService sut)
    {
        // Arrange
        const ushort expectedSeconds = 20;

        // Act
        sut.SetMaxStalenessInSeconds(serviceName, expectedSeconds);

        // Assert
        var fieldInfoForStaleness = typeof(BackgroundServiceHealthService).GetField(
            "maxStalenessInSeconds",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(fieldInfoForStaleness);

        var maxStalenessDict = (ConcurrentDictionary<string, ushort>)fieldInfoForStaleness.GetValue(sut)!;
        Assert.True(maxStalenessDict.ContainsKey(serviceName));

        var actual = maxStalenessDict[serviceName];

        Assert.Equal(expectedSeconds, actual);
    }

    [Theory]
    [InlineData("TimeFileWorker", true, true)]
    [InlineData("TimeFileWorker", false, false)]
    public void SetRunningStateTest(
        string serviceName,
        bool expected,
        bool isRunning)
    {
        // Arrange
        var sut = new BackgroundServiceHealthService(new FakeTimeProvider());

        // Act & Assert
        Assert.False(sut.IsServiceRunning(serviceName));

        sut.SetRunningState(serviceName, isRunning);

        Assert.Equal(expected, sut.IsServiceRunning(serviceName));
    }

    [Theory]
    [InlineData("TimeFileWorker", true, true, null, null)]
    [InlineData("TimeFileWorker", true, true, null, 2)]
    [InlineData("TimeFileWorker", true, true, (ushort)2, 2)]
    [InlineData("TimeFileWorker", true, true, (ushort)2, 1)]
    [InlineData("TimeFileWorker", true, true, (ushort)2, 0)]
    [InlineData("TimeFileWorker", true, true, (ushort)0, 0)]
    [InlineData("TimeFileWorker", true, true, (ushort)0, 1)]
    [InlineData("TimeFileWorker", true, true, (ushort)2, null)]
    [InlineData("TimeFileWorker", false, true, (ushort)2, 3)]
    [InlineData("TimeFileWorker", false, true, (ushort)0, 2)]
    [InlineData("TimeFileWorker", false, false, null, null)]
    [InlineData("TimeFileWorker", false, false, (ushort)2, 0)]
    [InlineData("TimeFileWorker", false, false, null, 2)]
    [InlineData("TimeFileWorker", false, false, (ushort)2, 3)]
    [InlineData("TimeFileWorker", false, false, (ushort)2, null)]
    public void IsServiceRunningTest(
        string serviceName,
        bool expected,
        bool isRunning,
        ushort? maxStalenessInSeconds,
        int? secondsToAdvance)
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var sut = new BackgroundServiceHealthService(timeProvider);

        if (maxStalenessInSeconds.HasValue)
        {
            sut.SetMaxStalenessInSeconds(serviceName, maxStalenessInSeconds.Value);
        }

        sut.SetRunningState(serviceName, isRunning);

        // Act
        if (secondsToAdvance.HasValue)
        {
            timeProvider.Advance(TimeSpan.FromSeconds(secondsToAdvance.Value + GracePeriodInSeconds));
        }

        // Assert
        Assert.Equal(expected, sut.IsServiceRunning(serviceName));
    }

    [Fact]
    public void Constructor_NullTimeProvider_Throws()
        => Assert.Throws<ArgumentNullException>(() => new BackgroundServiceHealthService(null!));
}