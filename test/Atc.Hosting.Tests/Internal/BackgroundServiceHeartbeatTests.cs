namespace Atc.Hosting.Tests.Internal;

public sealed class BackgroundServiceHeartbeatTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 1)]
    [InlineData(4, 2)]
    [InlineData(20, 10)]
    [InlineData(60, 10)]
    [InlineData(ushort.MaxValue, 10)]
    public void GetInterval_IsHalfTheRepeatInterval_ClampedBetweenOneAndTenSeconds(
        int repeatIntervalSeconds,
        int expectedSeconds)
    {
        // Arrange
        var options = new DefaultBackgroundServiceOptions { RepeatIntervalSeconds = (ushort)repeatIntervalSeconds };

        // Act
        var actual = BackgroundServiceHeartbeat.GetInterval(options);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), actual);
    }

    [Fact]
    public void ScheduleInterval_IsTenSeconds()
        => Assert.Equal(TimeSpan.FromSeconds(10), BackgroundServiceHeartbeat.ScheduleInterval);

    [Theory]
    [InlineData(1, 3)]
    [InlineData(2, 6)]
    [InlineData(10, 30)]
    public void GetMaxStalenessInSeconds_IsThreeIntervals(
        int intervalSeconds,
        int expected)
        => Assert.Equal(expected, BackgroundServiceHeartbeat.GetMaxStalenessInSeconds(TimeSpan.FromSeconds(intervalSeconds)));
}