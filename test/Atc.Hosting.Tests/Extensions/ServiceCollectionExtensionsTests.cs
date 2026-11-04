namespace Atc.Hosting.Tests.Extensions;

public sealed class ServiceCollectionExtensionsTests
{
    private const string SectionName = "Worker";

    [Fact]
    public void AddBackgroundService_Enabled_RegistersOptionsHostedServiceRegistrationAndHealthService()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new MyServiceOptions { RepeatIntervalSeconds = 20 };

        // Act
        services.AddBackgroundService<MyWorkerService, MyServiceOptions>(options);

        // Assert
        Assert.Single(
            services,
            d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(MyWorkerService));

        using var provider = services.BuildServiceProvider();

        Assert.Same(options, provider.GetRequiredService<MyServiceOptions>());
        Assert.Same(options, provider.GetRequiredService<IOptions<MyServiceOptions>>().Value);
        Assert.Equal(
            new BackgroundServiceRegistration(nameof(MyWorkerService), typeof(MyWorkerService), MaxStalenessInSeconds: 30),
            Assert.Single(provider.GetServices<BackgroundServiceRegistration>()));

        Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
        Assert.IsType<BackgroundServiceHealthService>(provider.GetRequiredService<IBackgroundServiceHealthService>());
    }

    [Fact]
    public void AddBackgroundService_Disabled_RegistersOnlyTheOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new MyServiceOptions { Enabled = false };

        // Act
        services.AddBackgroundService<MyWorkerService, MyServiceOptions>(options);

        // Assert
        using var provider = services.BuildServiceProvider();

        Assert.Same(options, provider.GetRequiredService<MyServiceOptions>());
        Assert.Same(options, provider.GetRequiredService<IOptions<MyServiceOptions>>().Value);
        Assert.Empty(provider.GetServices<IHostedService>());
        Assert.Empty(provider.GetServices<BackgroundServiceRegistration>());
        Assert.Null(provider.GetService<IBackgroundServiceHealthService>());
    }

    [Fact]
    public void AddBackgroundService_Configuration_BindsAllOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [$"{SectionName}:Enabled"] = "false",
            [$"{SectionName}:StartupDelaySeconds"] = "5",
            [$"{SectionName}:RepeatIntervalSeconds"] = "15",
            [$"{SectionName}:MaxWorkDurationSeconds"] = "60",
            [$"{SectionName}:MaxConsecutiveFailures"] = "3",
            [$"{SectionName}:ServiceName"] = "custom-name",
        });

        // Act
        services.AddBackgroundService<MyWorkerService, MyServiceOptions>(configuration.GetSection(SectionName));

        // Assert
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<MyServiceOptions>();

        Assert.False(options.Enabled);
        Assert.Equal(5, options.StartupDelaySeconds);
        Assert.Equal(15, options.RepeatIntervalSeconds);
        Assert.Equal((ushort)60, options.MaxWorkDurationSeconds);
        Assert.Equal((ushort)3, options.MaxConsecutiveFailures);
        Assert.Equal("custom-name", options.ServiceName);
    }

    [Fact]
    public void AddBackgroundService_ConfigurationWithoutEnabled_IsEnabled()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [$"{SectionName}:RepeatIntervalSeconds"] = "15",
        });

        // Act
        services.AddBackgroundService<MyWorkerService, MyServiceOptions>(configuration.GetSection(SectionName));

        // Assert
        Assert.Single(services, d => d.ServiceType == typeof(IHostedService));
    }

    [Fact]
    public void AddBackgroundService_MissingSection_UsesTheDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal));

        // Act
        services.AddBackgroundService<MyWorkerService, MyServiceOptions>(configuration.GetSection(SectionName));

        // Assert
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<MyServiceOptions>();
        var defaults = new MyServiceOptions();

        Assert.True(options.Enabled);
        Assert.Equal(defaults.RepeatIntervalSeconds, options.RepeatIntervalSeconds);
        Assert.Single(services, d => d.ServiceType == typeof(IHostedService));
    }

    [Fact]
    public void AddBackgroundService_ServiceName_IsUsedForTheRegistration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBackgroundService<MyWorkerService, MyServiceOptions>(new MyServiceOptions { ServiceName = "custom-name" });

        // Assert
        var registration = Assert.Single(GetRegistrations(services));

        Assert.Equal("custom-name", registration.ServiceName);
    }

    [Fact]
    public void AddBackgroundService_DuplicateServiceName_Throws()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddBackgroundService<MyWorkerService, MyServiceOptions>(new MyServiceOptions { ServiceName = "duplicate-name" });

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBackgroundService<MyTokenIgnoringWorkerService, MyOtherServiceOptions>(
                new MyOtherServiceOptions { ServiceName = "duplicate-name" }));

        // Assert
        Assert.Contains("duplicate-name", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddBackgroundService_SameServiceTypeTwice_Throws()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddBackgroundService<MyWorkerService, MyServiceOptions>(new MyServiceOptions { ServiceName = "first" });

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBackgroundService<MyWorkerService, MyOtherServiceOptions>(new MyOtherServiceOptions { ServiceName = "second" }));

        // Assert
        Assert.Contains(nameof(MyWorkerService), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddBackgroundService_DisabledDuplicate_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddBackgroundService<MyWorkerService, MyServiceOptions>(new MyServiceOptions());

        // Act
        services.AddBackgroundService<MyWorkerService, MyOtherServiceOptions>(new MyOtherServiceOptions { Enabled = false });

        // Assert
        Assert.Single(GetRegistrations(services));
    }

    [Fact]
    public void AddBackgroundService_HealthService_AppliesTheHeartbeatStaleness()
    {
        // Arrange - RepeatIntervalSeconds 2 gives a 1 s heartbeat and 3 s staleness (+ 1 s grace)
        var timeProvider = new FakeTimeProvider();
        var services = new ServiceCollection();

        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddBackgroundService<MyWorkerService, MyServiceOptions>(new MyServiceOptions { RepeatIntervalSeconds = 2 });

        using var provider = services.BuildServiceProvider();
        var healthService = provider.GetRequiredService<IBackgroundServiceHealthService>();

        healthService.SetRunningState(nameof(MyWorkerService), isRunning: true);

        // Act & Assert
        timeProvider.Advance(TimeSpan.FromSeconds(4));
        Assert.True(healthService.IsServiceRunning(nameof(MyWorkerService)));

        timeProvider.Advance(TimeSpan.FromSeconds(0.5));
        Assert.False(healthService.IsServiceRunning(nameof(MyWorkerService)));
    }

    [Fact]
    public void AddBackgroundScheduleService_Enabled_RegistersTheScheduleHeartbeatStaleness()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var services = new ServiceCollection();

        services.AddSingleton<TimeProvider>(timeProvider);

        // Act
        services.AddBackgroundScheduleService<MyScheduleWorkerService, MyScheduleServiceOptions>(new MyScheduleServiceOptions());

        // Assert
        Assert.Single(
            services,
            d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(MyScheduleWorkerService));

        using var provider = services.BuildServiceProvider();
        var registration = Assert.Single(provider.GetServices<BackgroundServiceRegistration>());

        Assert.Equal(
            new BackgroundServiceRegistration(nameof(MyScheduleWorkerService), typeof(MyScheduleWorkerService), MaxStalenessInSeconds: 30),
            registration);

        var healthService = provider.GetRequiredService<IBackgroundServiceHealthService>();

        healthService.SetRunningState(registration.ServiceName, isRunning: true);
        timeProvider.Advance(TimeSpan.FromSeconds(31));
        Assert.True(healthService.IsServiceRunning(registration.ServiceName));

        timeProvider.Advance(TimeSpan.FromSeconds(0.5));
        Assert.False(healthService.IsServiceRunning(registration.ServiceName));
    }

    [Fact]
    public void AddBackgroundScheduleService_Configuration_BindsTheCronExpression()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [$"{SectionName}:CronExpression"] = "0 * * * *",
        });

        // Act
        services.AddBackgroundScheduleService<MyScheduleWorkerService, MyScheduleServiceOptions>(configuration.GetSection(SectionName));

        // Assert
        using var provider = services.BuildServiceProvider();

        Assert.Equal("0 * * * *", provider.GetRequiredService<MyScheduleServiceOptions>().CronExpression);
    }

    [Fact]
    public void AddBackgroundService_OwnHealthServiceRegisteredFirst_IsKept()
    {
        // Arrange
        var ownHealthService = Substitute.For<IBackgroundServiceHealthService>();
        var services = new ServiceCollection();

        services.AddSingleton(ownHealthService);

        // Act
        services.AddBackgroundService<MyWorkerService, MyServiceOptions>(new MyServiceOptions());

        // Assert
        Assert.Single(services, d => d.ServiceType == typeof(IBackgroundServiceHealthService));

        using var provider = services.BuildServiceProvider();

        Assert.Same(ownHealthService, provider.GetRequiredService<IBackgroundServiceHealthService>());
    }

    [Fact]
    public void AddBackgroundServiceHealthService_AppliesTheGivenStaleness()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var services = new ServiceCollection();

        services.AddSingleton<TimeProvider>(timeProvider);

        // Act
        services.AddBackgroundServiceHealthService(("a", 10), ("b", 20));
        services.AddBackgroundServiceHealthService(("c", 5));

        // Assert
        Assert.Single(services, d => d.ServiceType == typeof(IBackgroundServiceHealthService));

        using var provider = services.BuildServiceProvider();
        var healthService = provider.GetRequiredService<IBackgroundServiceHealthService>();

        healthService.SetRunningState("a", isRunning: true);
        healthService.SetRunningState("b", isRunning: true);
        timeProvider.Advance(TimeSpan.FromSeconds(11.5));

        Assert.False(healthService.IsServiceRunning("a"));
        Assert.True(healthService.IsServiceRunning("b"));
    }

    [Fact]
    public void AddBackgroundServiceHealthService_RegistersTheTimeProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBackgroundServiceHealthService();

        // Assert
        using var provider = services.BuildServiceProvider();

        Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
        Assert.IsType<BackgroundServiceHealthService>(provider.GetRequiredService<IBackgroundServiceHealthService>());
    }

    [Fact]
    public void AddBackgroundServiceHealthService_NameOfARegisteredService_Throws()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddBackgroundService<MyWorkerService, MyServiceOptions>(new MyServiceOptions { ServiceName = "x" });

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBackgroundServiceHealthService(("x", 10)));

        // Assert
        Assert.Contains("'x'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddBackgroundServiceHealthService_SameNameTwice_Throws()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddBackgroundServiceHealthService(("x", 10));

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBackgroundServiceHealthService(("x", 20)));

        // Assert
        Assert.Contains("'x'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddBackgroundService_NameAlreadyPassedToTheHealthService_Throws()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddBackgroundServiceHealthService(("x", 10));

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBackgroundService<MyWorkerService, MyServiceOptions>(new MyServiceOptions { ServiceName = "x" }));

        // Assert
        Assert.Contains("'x'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddBackgroundServiceHealthService_EmptyName_Throws()
        => Assert.Throws<ArgumentException>(
            () => new ServiceCollection().AddBackgroundServiceHealthService((" ", 10)));

    [Fact]
    public void AddBackgroundService_KeyedRegistrationInTheCollection_IsIgnored()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddKeyedSingleton(
            "some-key",
            new BackgroundServiceRegistration("x", typeof(MyWorkerService), MaxStalenessInSeconds: 10));

        // Act
        services.AddBackgroundService<MyWorkerService, MyServiceOptions>(new MyServiceOptions { ServiceName = "x" });

        // Assert
        using var provider = services.BuildServiceProvider();

        Assert.Equal("x", Assert.Single(provider.GetServices<BackgroundServiceRegistration>()).ServiceName);
    }

    [Fact]
    public void AddBackgroundService_AllOptionsInterfaces_ResolveTheRegisteredInstance()
    {
        // Arrange - the generic host calls AddOptions(), so IOptionsMonitor<> and IOptionsSnapshot<> are available
        var services = new ServiceCollection();
        var options = new MyServiceOptions { ServiceName = "custom-name" };

        services.AddOptions();

        // Act
        services.AddBackgroundService<MyWorkerService, MyServiceOptions>(options);

        // Assert
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.Same(options, provider.GetRequiredService<IOptions<MyServiceOptions>>().Value);
        Assert.Same(options, provider.GetRequiredService<IOptionsMonitor<MyServiceOptions>>().CurrentValue);
        Assert.Same(options, scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<MyServiceOptions>>().Value);
    }

    [Fact]
    public void AddBackgroundService_ValidOptions_PassTheirValidators()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new MyServiceOptions { RepeatIntervalSeconds = 20 };

        services
            .AddOptions<MyServiceOptions>()
            .Validate(o => o.RepeatIntervalSeconds > 10, "Too short");

        services.AddBackgroundService<MyWorkerService, MyServiceOptions>(options);

        // Act
        using var provider = services.BuildServiceProvider();

        // Assert
        Assert.Same(options, provider.GetRequiredService<IOptions<MyServiceOptions>>().Value);
    }

    [Fact]
    public void AddBackgroundService_InvalidOptions_FailForEveryOptionsInterface()
    {
        // Arrange - the validator is added after the registration, so the factory must read it lazily
        var services = new ServiceCollection();

        services.AddBackgroundService<MyWorkerService, MyServiceOptions>(new MyServiceOptions { RepeatIntervalSeconds = 1 });

        services
            .AddOptions<MyServiceOptions>()
            .Validate(o => o.RepeatIntervalSeconds > 10, "Too short");

        // Act
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Assert
        var exceptions = new[]
        {
            Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<MyServiceOptions>>().Value),
            Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptionsMonitor<MyServiceOptions>>().CurrentValue),
            Assert.Throws<OptionsValidationException>(() => scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<MyServiceOptions>>().Value),
        };

        Assert.All(exceptions, e => Assert.Contains("Too short", e.Failures, StringComparer.Ordinal));
    }

    [Fact]
    public void AddBackgroundService_InvalidOptions_FailValidateOnStart()
    {
        // Arrange
        var services = new ServiceCollection();

        services
            .AddOptions<MyServiceOptions>()
            .Validate(o => o.RepeatIntervalSeconds > 10, "Too short")
            .ValidateOnStart();

        services.AddBackgroundService<MyWorkerService, MyServiceOptions>(new MyServiceOptions { RepeatIntervalSeconds = 1 });

        // Act
        using var provider = services.BuildServiceProvider();
        var startupValidator = provider.GetRequiredService<IStartupValidator>();

        // Assert
        var exception = Assert.Throws<OptionsValidationException>(startupValidator.Validate);

        Assert.Contains("Too short", exception.Failures, StringComparer.Ordinal);
    }

    [Fact]
    public void AddBackgroundService_ConfigureCall_IsIgnored()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new MyServiceOptions { ServiceName = "bound-name" };

        services.AddBackgroundService<MyWorkerService, MyServiceOptions>(options);
        services.Configure<MyServiceOptions>(o => o.ServiceName = "configured-name");

        // Act
        using var provider = services.BuildServiceProvider();

        // Assert
        Assert.Equal("bound-name", provider.GetRequiredService<IOptions<MyServiceOptions>>().Value.ServiceName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddBackgroundService_SameOptionsTypeForTwoServices_Throws(
        bool firstEnabled)
    {
        // Arrange - a disabled service counts too, because its options are registered as well
        var services = new ServiceCollection();

        services.AddBackgroundService<MyWorkerService, MyServiceOptions>(
            new MyServiceOptions { ServiceName = "first", Enabled = firstEnabled });

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBackgroundService<MyTokenIgnoringWorkerService, MyServiceOptions>(
                new MyServiceOptions { ServiceName = "second" }));

        // Assert
        Assert.Contains(nameof(MyServiceOptions), exception.Message, StringComparison.Ordinal);
        Assert.Contains("'first'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddBackgroundScheduleService_ResolvedHostedService_UsesTheRegisteredOptionsAndName()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddBackgroundScheduleService<MyScheduleWorkerService, MyScheduleServiceOptions>(
            new MyScheduleServiceOptions { ServiceName = "custom-name" });

        // Act
        using var provider = services.BuildServiceProvider();
        var hostedService = Assert.IsType<MyScheduleWorkerService>(Assert.Single(provider.GetServices<IHostedService>()));

        // Assert - the name the worker reports under is the name its health check and staleness use
        var registration = Assert.Single(provider.GetServices<BackgroundServiceRegistration>());

        Assert.Equal("custom-name", hostedService.ServiceName);
        Assert.Equal(registration.ServiceName, hostedService.ServiceName);
        Assert.Same(provider.GetRequiredService<MyScheduleServiceOptions>(), hostedService.ServiceOptions);
    }

    [Fact]
    public void AddBackgroundService_ServiceDerivedFromAnotherWorker_UsesTheNameItReportsUnder()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new MyServiceOptions();

        // Act
        services.AddBackgroundService<MyDerivedWorkerService, MyServiceOptions>(options);

        // Assert
        var registration = Assert.Single(GetRegistrations(services));
        using var worker = new MyDerivedWorkerService(
            NullLogger<MyWorkerService>.Instance,
            Options.Create(options),
            Task.CompletedTask);

        Assert.Equal(nameof(MyWorkerService), registration.ServiceName);
        Assert.Equal(worker.ServiceName, registration.ServiceName);
        Assert.Equal(typeof(MyDerivedWorkerService), registration.ServiceType);
    }

    [Fact]
    public void AddBackgroundScheduleService_ServiceDerivedFromAnotherWorker_UsesTheNameItReportsUnder()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        // Act
        services.AddBackgroundScheduleService<MyDerivedScheduleWorkerService, MyScheduleServiceOptions>(
            new MyScheduleServiceOptions());

        // Assert
        using var provider = services.BuildServiceProvider();
        var worker = Assert.IsType<MyDerivedScheduleWorkerService>(Assert.Single(provider.GetServices<IHostedService>()));
        var registration = Assert.Single(provider.GetServices<BackgroundServiceRegistration>());

        Assert.Equal(nameof(MyScheduleWorkerService), registration.ServiceName);
        Assert.Equal(worker.ServiceName, registration.ServiceName);
    }

    [Fact]
    public void AddBackgroundService_ServiceWithoutABaseClass_UsesTheServiceTypeName()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBackgroundService<MyPlainHostedService, MyServiceOptions>(new MyServiceOptions());

        // Assert
        var registration = Assert.Single(GetRegistrations(services));

        Assert.Equal(nameof(MyPlainHostedService), registration.ServiceName);
    }

    private static IConfiguration CreateConfiguration(
        Dictionary<string, string?> values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

    private static IEnumerable<BackgroundServiceRegistration> GetRegistrations(
        IServiceCollection services)
        => services
            .Where(d => !d.IsKeyedService)
            .Select(d => d.ImplementationInstance)
            .OfType<BackgroundServiceRegistration>();
}