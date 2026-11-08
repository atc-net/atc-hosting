namespace Atc.Hosting.HealthChecks.Tests;

public sealed class HealthChecksBuilderExtensionsTests
{
    [Fact]
    public void AddBackgroundServices_RegisteredServices_AddsOneCheckPerService()
    {
        // Arrange
        var services = new ServiceCollection();
        var timeout = TimeSpan.FromSeconds(5);

        services.AddBackgroundService<MyHostedService, DefaultBackgroundServiceOptions>(new DefaultBackgroundServiceOptions());
        services.AddBackgroundScheduleService<MyOtherHostedService, DefaultBackgroundScheduleServiceOptions>(
            new DefaultBackgroundScheduleServiceOptions { ServiceName = "custom-name" });

        // Act
        services
            .AddHealthChecks()
            .AddBackgroundServices(HealthStatus.Degraded, ["dependency"], timeout);

        // Assert
        var registrations = GetRegistrations(services);

        Assert.Equal(
            [nameof(MyHostedService), "custom-name"],
            registrations.Select(r => r.Name),
            StringComparer.Ordinal);

        Assert.All(
            registrations,
            r =>
            {
                Assert.Equal(HealthStatus.Degraded, r.FailureStatus);
                Assert.Equal(timeout, r.Timeout);
                Assert.Equal("dependency", Assert.Single(r.Tags));
            });
    }

    [Fact]
    public void AddBackgroundServices_DisabledService_AddsNoCheckForIt()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddBackgroundService<MyHostedService, DefaultBackgroundServiceOptions>(new DefaultBackgroundServiceOptions());
        services.AddBackgroundScheduleService<MyOtherHostedService, DefaultBackgroundScheduleServiceOptions>(
            new DefaultBackgroundScheduleServiceOptions { Enabled = false });

        // Act
        services
            .AddHealthChecks()
            .AddBackgroundServices();

        // Assert
        var registration = Assert.Single(GetRegistrations(services));

        Assert.Equal(nameof(MyHostedService), registration.Name);
    }

    [Fact]
    public void AddBackgroundServices_Names_AddsExactlyThoseChecks()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddBackgroundService<MyHostedService, DefaultBackgroundServiceOptions>(new DefaultBackgroundServiceOptions());

        // Act
        services
            .AddHealthChecks()
            .AddBackgroundServices(["first", "second"], tags: ["dependency"]);

        // Assert
        var registrations = GetRegistrations(services);

        Assert.Equal(["first", "second"], registrations.Select(r => r.Name), StringComparer.Ordinal);
        Assert.All(registrations, r => Assert.Equal("dependency", Assert.Single(r.Tags)));
    }

    [Fact]
    public void AddBackgroundServices_CalledBeforeTheRegistrations_StillAddsTheirChecks()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services
            .AddHealthChecks()
            .AddBackgroundServices();

        services.AddBackgroundService<MyHostedService, DefaultBackgroundServiceOptions>(new DefaultBackgroundServiceOptions());

        // Assert
        var registration = Assert.Single(GetRegistrations(services));

        Assert.Equal(nameof(MyHostedService), registration.Name);
    }

    [Fact]
    public void AddBackgroundServices_TagsChangedAfterTheCall_KeepsTheOriginalTags()
    {
        // Arrange
        var services = new ServiceCollection();
        var tags = new List<string> { "dependency" };

        services.AddBackgroundService<MyHostedService, DefaultBackgroundServiceOptions>(new DefaultBackgroundServiceOptions());

        // Act
        services
            .AddHealthChecks()
            .AddBackgroundServices(tags: tags);

        tags.Add("added-later");

        // Assert
        var registration = Assert.Single(GetRegistrations(services));

        Assert.Equal("dependency", Assert.Single(registration.Tags));
    }

    [Fact]
    public void AddBackgroundServices_KeyedRegistrationInTheCollection_IsIgnored()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddKeyedSingleton("key", new BackgroundServiceRegistration("keyed", typeof(MyOtherHostedService), MaxStalenessInSeconds: 30));
        services.AddBackgroundService<MyHostedService, DefaultBackgroundServiceOptions>(new DefaultBackgroundServiceOptions());

        // Act
        services
            .AddHealthChecks()
            .AddBackgroundServices();

        // Assert
        var registration = Assert.Single(GetRegistrations(services));

        Assert.Equal(nameof(MyHostedService), registration.Name);
    }

    [Fact]
    public async Task AddBackgroundServices_RunningAndStoppedService_ReportsThroughTheHealthCheckService()
    {
        // Arrange
        var services = new ServiceCollection();
        var cancellationToken = TestContext.Current.CancellationToken;

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddBackgroundService<MyHostedService, DefaultBackgroundServiceOptions>(new DefaultBackgroundServiceOptions());
        services.AddBackgroundScheduleService<MyOtherHostedService, DefaultBackgroundScheduleServiceOptions>(
            new DefaultBackgroundScheduleServiceOptions());

        services
            .AddHealthChecks()
            .AddBackgroundServices();

        await using var provider = services.BuildServiceProvider();

        provider
            .GetRequiredService<IBackgroundServiceHealthService>()
            .SetRunningState(nameof(MyHostedService), isRunning: true);

        // Act
        var report = await provider
            .GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(cancellationToken);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, report.Status);
        Assert.Equal(HealthStatus.Healthy, report.Entries[nameof(MyHostedService)].Status);
        Assert.Equal(HealthStatus.Unhealthy, report.Entries[nameof(MyOtherHostedService)].Status);
        Assert.Equal("Background service 'MyOtherHostedService' is unhealthy.", report.Entries[nameof(MyOtherHostedService)].Description);
    }

    private static List<HealthCheckRegistration> GetRegistrations(
        ServiceCollection services)
    {
        using var provider = services.BuildServiceProvider();

        return provider
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value
            .Registrations
            .ToList();
    }
}