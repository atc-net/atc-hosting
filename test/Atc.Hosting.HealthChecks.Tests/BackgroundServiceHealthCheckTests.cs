namespace Atc.Hosting.HealthChecks.Tests;

public sealed class BackgroundServiceHealthCheckTests
{
    private const string ServiceName = "my-service";

    [Fact]
    public async Task CheckHealthAsync_ServiceIsRunning_ReturnsHealthy()
    {
        // Arrange
        var healthService = Substitute.For<IBackgroundServiceHealthService>();
        healthService
            .IsServiceRunning(ServiceName)
            .Returns(true);

        var healthCheck = new BackgroundServiceHealthCheck(healthService);

        // Act
        var result = await healthCheck.CheckHealthAsync(
            CreateContext(healthCheck, HealthStatus.Unhealthy),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Background service 'my-service' is healthy", result.Description);
    }

    [Theory]
    [InlineData(HealthStatus.Unhealthy)]
    [InlineData(HealthStatus.Degraded)]
    public async Task CheckHealthAsync_ServiceIsNotRunning_ReturnsTheFailureStatus(
        HealthStatus failureStatus)
    {
        // Arrange
        var healthService = Substitute.For<IBackgroundServiceHealthService>();
        healthService
            .IsServiceRunning(ServiceName)
            .Returns(false);

        var healthCheck = new BackgroundServiceHealthCheck(healthService);

        // Act
        var result = await healthCheck.CheckHealthAsync(
            CreateContext(healthCheck, failureStatus),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(failureStatus, result.Status);
        Assert.Equal("Background service 'my-service' is unhealthy.", result.Description);
    }

    private static HealthCheckContext CreateContext(
        IHealthCheck healthCheck,
        HealthStatus failureStatus)
        => new()
        {
            Registration = new HealthCheckRegistration(
                ServiceName,
                healthCheck,
                failureStatus,
                tags: null),
        };
}