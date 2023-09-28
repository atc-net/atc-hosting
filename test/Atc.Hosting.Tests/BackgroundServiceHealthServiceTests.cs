namespace Atc.Hosting.Tests;

public sealed class BackgroundServiceHealthServiceTests
{
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
    [InlineAutoNSubstituteData("TimeFileWorker", true, true)]
    [InlineAutoNSubstituteData("TimeFileWorker", false, false)]
    public void SetRunningStateTest(
        string serviceName,
        bool expected,
        bool isRunning,
        [Frozen] ITimeProvider timeProvider)
    {
        // Arrange
        var frozenTime = DateTime.UtcNow;

        timeProvider
            .UtcNow
            .Returns(frozenTime);

        var sut = new BackgroundServiceHealthService(timeProvider);

        // Act & Assert
        Assert.False(sut.IsServiceRunning(serviceName));
        sut.SetRunningState(serviceName, isRunning);

        var fieldInfoForServiceStates = typeof(BackgroundServiceHealthService).GetField(
            "serviceStates",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(fieldInfoForServiceStates);

        var serviceStatesDict = (ConcurrentDictionary<string, (bool IsRunning, DateTime LastUpdated)>)fieldInfoForServiceStates.GetValue(sut)!;
        Assert.True(serviceStatesDict.ContainsKey(serviceName));

        var actual = serviceStatesDict[serviceName].IsRunning;

        Assert.Equal(expected, actual);
        Assert.NotEqual(DateTime.MinValue, serviceStatesDict[serviceName].LastUpdated);
    }

    [Theory]
    [InlineAutoNSubstituteData("TimeFileWorker", false, true, null, null)]
    [InlineAutoNSubstituteData("TimeFileWorker", false, false, null, null)]
    [InlineAutoNSubstituteData("TimeFileWorker", true, true, (ushort)2, 2)]
    [InlineAutoNSubstituteData("TimeFileWorker", true, true, (ushort)2, 1)]
    [InlineAutoNSubstituteData("TimeFileWorker", true, true, (ushort)2, 0)]
    [InlineAutoNSubstituteData("TimeFileWorker", true, true, (ushort)0, 0)]
    [InlineAutoNSubstituteData("TimeFileWorker", true, true, (ushort)0, 1)]
    [InlineAutoNSubstituteData("TimeFileWorker", true, true, (ushort)2, null)]
    [InlineAutoNSubstituteData("TimeFileWorker", false, true, (ushort)2, 3)]
    [InlineAutoNSubstituteData("TimeFileWorker", false, true, (ushort)0, 2)]
    [InlineAutoNSubstituteData("TimeFileWorker", false, true, null, 2)]
    [InlineAutoNSubstituteData("TimeFileWorker", false, false, (ushort)2, 0)]
    [InlineAutoNSubstituteData("TimeFileWorker", false, false, null, 2)]
    [InlineAutoNSubstituteData("TimeFileWorker", false, false, (ushort)2, 3)]
    [InlineAutoNSubstituteData("TimeFileWorker", false, false, (ushort)2, null)]
    public void IsServiceRunningTest(
        string serviceName,
        bool expected,
        bool isRunning,
        ushort? maxStalenessInSeconds,
        int? secondsToAdvance,
        [Frozen] ITimeProvider timeProvider)
    {
        // Arrange
        const ushort gracePeriodInSeconds = 1;
        var frozenTime = DateTime.UtcNow;

        timeProvider
            .UtcNow
            .Returns(frozenTime);

        var sut = new BackgroundServiceHealthService(timeProvider);

        // Act & Assert
        if (maxStalenessInSeconds.HasValue)
        {
            sut.SetMaxStalenessInSeconds(serviceName, maxStalenessInSeconds.Value);
        }

        sut.SetRunningState(serviceName, isRunning);

        if (secondsToAdvance.HasValue)
        {
            var fieldInfoForServiceStates = typeof(BackgroundServiceHealthService).GetField(
                "serviceStates",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(fieldInfoForServiceStates);

            var serviceStatesDict = (ConcurrentDictionary<string, (bool IsRunning, DateTime LastUpdated)>)fieldInfoForServiceStates.GetValue(sut)!;
            Assert.True(serviceStatesDict.ContainsKey(serviceName));

            var frozenTimeRewind = frozenTime.AddSeconds(-(secondsToAdvance.Value + gracePeriodInSeconds));

            serviceStatesDict[serviceName] = (serviceStatesDict[serviceName].IsRunning, frozenTimeRewind);
        }

        Assert.Equal(expected, sut.IsServiceRunning(serviceName));
    }
}