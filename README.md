[![NuGet Version](https://img.shields.io/nuget/v/atc.hosting.svg?logo=nuget&style=for-the-badge)](https://www.nuget.org/packages/atc.hosting)

# Atc.Hosting

The Atc.Hosting namespace serves as a toolbox for building scalable and reliable hosting solutions, with an emphasis on background services. It contains classes and extension methods designed to handle common hosting scenarios, providing enhanced features like custom logging, retries, and advanced configuration options. The namespace aims to streamline development efforts and improve the maintainability of hosted applications.

# Table of Contents

- [Atc.Hosting](#atchosting)
- [Table of Contents](#table-of-contents)
- [BackgroundServiceBase`<T>`](#backgroundservicebaset)
  - [Features](#features)
    - [Logging](#logging)
    - [Retry Mechanism](#retry-mechanism)
    - [Error Handling](#error-handling)
    - [Configuration Options](#configuration-options)
    - [Ease of Use](#ease-of-use)
  - [Sample Usage](#sample-usage)
  - [Setup BackgroundService via Dependency Injection](#setup-backgroundservice-via-dependency-injection)
- [BackgroundServiceHealthService](#backgroundservicehealthservice)
  - [Methods](#methods)
  - [Setup via Dependency Injection](#setup-via-dependency-injection)
  - [Using in Background Services](#using-in-background-services)
    - [Constructor dependency injection](#constructor-dependency-injection)
    - [In your `StartAsync` and `StopAsync` methods, update the service's running state](#in-your-startasync-and-stopasync-methods-update-the-services-running-state)
    - [Inside your worker method, you can set the running state of the service (to update latest timestamp)](#inside-your-worker-method-you-can-set-the-running-state-of-the-service-to-update-latest-timestamp)
- [Complete TimeFileWorker example](#complete-timefileworker-example)
- [Extensions for ServiceProvider](#extensions-for-serviceprovider)
  - [Using GetHostedService`<T>`](#using-gethostedservicet)
- [Requirements](#requirements)
- [How to contribute](#how-to-contribute)

# BackgroundServiceBase`<T>`

The `BackgroundServiceBase<T>` class serves as a base for continuous long running background services that require enhanced features like custom logging and configurable service options. It extends the ASP.NET Core's `BackgroundService` class, providing a more robust framework for handling background tasks.

## Features

### Logging

- Utilizes `ILogger<T>` for type-specific, high-performance logging.
- Automatically enriches log entries with the name of the service type (`T`).

### Error Handling

- Catches unhandled exceptions and logs them with a severity of `LogLevel.Warning`.
- Reruns the `DoWorkAsync` method after a configurable repeat interval.
- Designed to log errors rather than crashing the service.

### Configuration Options

- Allows for startup delays.
- Configurable repeat interval for running tasks.

### Ease of Use

- Simple to derive from and implement your work in the `DoWorkAsync` method.

## Sample Usage

```csharp
public class MyBackgroundService : BackgroundServiceBase<MyBackgroundService>
{
    public MyBackgroundService(ILogger<MyBackgroundService> logger, IBackgroundServiceOptions options)
        : base(logger, options)
    {
    }

    public override Task DoWorkAsync(CancellationToken stoppingToken)
    {
        // Your background task logic here
    }
}
```

## Setup BackgroundService via Dependency Injection

```csharp
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var host = Host
    .CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<ITimeService, TimeService>();
        services.Configure<TimeFileWorkerOptions>(configuration.GetSection(TimeFileWorkerOptions.SectionName));
        services.AddHostedService<TimeFileWorker>();
    })
    .Build();

host.Run();
```

In this example the `TimeFileWorker` BackgroundService is wired up by using `AddHostedService<T>` as a normal `BackgroundService`.

Note: `TimeFileWorker` uses `TimeFileWorkerOptions` that implements `IBackgroundServiceOptions`.

# BackgroundServiceHealthService

`IBackgroundServiceHealthService` is an interface that provides methods to manage and monitor the health of background services in a .NET application.

## Methods

- `SetMaxStalenessInSeconds(string serviceName, ushort seconds)`: Set the maximum allowed staleness duration for a service in seconds.
- `SetRunningState(string serviceName, bool isRunning)`: Update the running state of a service.
- `IsServiceRunning(string serviceName)`: Check if a service is running or not.

## Setup via Dependency Injection

Include the following code snippet in your startup to wire up the BackgroundService incl. HealthService.

```csharp
var host = Host
    .CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        //...existing dependency injection

        services.AddSingleton<IBackgroundServiceHealthService, BackgroundServiceHealthService>(s =>
        {
            var healthService = new BackgroundServiceHealthService(s.GetRequiredService<ITimeProvider>());
            var timeFileWorkerOptions = s.GetRequiredService<IOptions<TimeFileWorkerOptions>>().Value;
            healthService.SetMaxStalenessInSeconds(nameof(TimeFileWorker), timeFileWorkerOptions.RepeatIntervalSeconds);
            return healthService;
        });
    })
    .Build();
```

Here, the maximum staleness for `TimeFileWorker` is set using its `RepeatIntervalSeconds`.

## Using in Background Services

You can utilize `IBackgroundServiceHealthService` within your background services as shown below.

### Constructor dependency injection

```csharp
public TimeFileWorker(
    //...other parameters
    IBackgroundServiceHealthService healthService,
    //...other parameters
)
{
    this.healthService = healthService;
    //...other initializations
}
```

### In your `StartAsync` and `StopAsync` methods, update the service's running state

```csharp
public override async Task StartAsync(CancellationToken cancellationToken)
{
    await base.StartAsync(cancellationToken);
    healthService.SetRunningState(nameof(TimeFileWorker), isRunning: true);
    //...other code
}

public override async Task StopAsync(CancellationToken cancellationToken)
{
    //...other code
    await base.StopAsync(cancellationToken);
    healthService.SetRunningState(nameof(TimeFileWorker), isRunning: false);
}
```

### Inside your worker method, you can set the running state of the service (to update latest timestamp)

```csharp
public override async Task DoWorkAsync(CancellationToken stoppingToken)
{
    //...other code
    healthService.SetRunningState(nameof(TimeFileWorker), isRunning: true);
}
```

# Complete TimeFileWorker example

A sample reference implementation can be found in the sample project [`Atc.Hosting.TimeFile.Sample`](sample/Atc.Hosting.TimeFile.Sample/Program.cs)
which shows an example of the service `TimeFileWorker` that uses `BackgroundServiceBase` and the `IBackgroundServiceHealthService`.

```csharp
public class TimeFileWorker : BackgroundServiceBase<TimeFileWorker>
{
    private readonly IBackgroundServiceHealthService healthService;
    private readonly ITimeProvider timeProvider;

    private readonly TimeFileWorkerOptions workerOptions;

    public TimeFileWorker(
        ILogger<TimeFileWorker> logger,
        IBackgroundServiceHealthService healthService,
        ITimeProvider timeProvider,
        IOptions<TimeFileWorkerOptions> workerOptions)
        : base(
            logger,
            workerOptions.Value)
    {
        this.healthService = healthService;
        this.timeProvider = timeProvider;
        this.workerOptions = workerOptions.Value;
    }

    public override async Task StartAsync(
        CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);
        healthService.SetRunningState(nameof(TimeFileWorker), isRunning: true);
    }

    public override async Task StopAsync(
        CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        healthService.SetRunningState(nameof(TimeFileWorker), isRunning: false);
    }

    public override async Task DoWorkAsync(
        CancellationToken stoppingToken)
    {
        var isServiceRunning = healthService.IsServiceRunning(nameof(TimeFileWorker));

        Directory.CreateDirectory(workerOptions.OutputDirectory);

        var time = timeProvider.UtcNow;

        var outFile = Path.Combine(
            workerOptions.OutputDirectory,
            $"{time:yyyy-MM-dd--HHmmss}-{isServiceRunning}.txt");

        await File.WriteAllTextAsync(outFile, $"{ServiceName}-{isServiceRunning}", stoppingToken);

        healthService.SetRunningState(nameof(TimeFileWorker), isRunning: true);
    }
}
```

# Extensions for ServiceProvider

Defined extensions methods for ServiceProvider:
> GetHostedService`<T>`

## Using GetHostedService`<T>`

Example on how to retrieve the BackgroundService from the HttpContext in a MVC controller or MinimalApi endpoint.

In this example we are working with the `TimeFileWorker` BackgroundService.

Note: Remember to wire up BackgroundService in `Program.cs` by adding this line `services.AddHostedService<TimeFileWorker>();`.

Example setup for a MVC controller:

```csharp
[HttpGet("my-method")]
public void GetMyMethod()
{
    var timeFileWorker = httpContext.RequestServices.GetHostedService<TimeFileWorker>();

    if (timeFileWorker is not null)
    {
      // Here we have access to the TimeFileWorker instance.
    }
}
```

Example setup for a MinimalApi endpoint:

```csharp
public void DefineEndpoints(WebApplication app)
{
    app.MapGet("my-method", async httpContext =>
    {
        var timeFileWorker = httpContext.RequestServices.GetHostedService<TimeFileWorker>();

        if (timeFileWorker is not null)
        {
          // Here we have access to the TimeFileWorker instance.
        }

        await Task.CompletedTask;
    });
}
```

# Requirements

- [.NET 7 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)

# How to contribute

[Contribution Guidelines](https://atc-net.github.io/introduction/about-atc#how-to-contribute)

[Coding Guidelines](https://atc-net.github.io/introduction/about-atc#coding-guidelines)
