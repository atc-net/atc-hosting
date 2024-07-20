[![NuGet Version](https://img.shields.io/nuget/v/atc.hosting.svg?logo=nuget&style=for-the-badge)](https://www.nuget.org/packages/atc.hosting)

# Atc.Hosting

The Atc.Hosting namespace serves as a toolbox for building scalable and reliable hosting solutions, with an emphasis on background services. It contains classes and extension methods designed to handle common hosting scenarios, providing enhanced features like custom logging, and advanced configuration options. The namespace aims to streamline development efforts and improve the maintainability of hosted applications.

# Table of Contents

- [Atc.Hosting](#atchosting)
- [Table of Contents](#table-of-contents)
- [BackgroundServiceBase`<T>`](#backgroundservicebaset)
  - [Features](#features)
    - [Logging](#logging)
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
  - [Default implementation for `BackgroundServiceBase<>`](#default-implementation-for-backgroundservicebase)
- [Complete TimeFileWorker example](#complete-timefileworker-example)
- [Extensions for ServiceProvider](#extensions-for-serviceprovider)
  - [Using GetHostedService`<T>`](#using-gethostedservicet)
- [Requirements](#requirements)
- [How to contribute](#how-to-contribute)

# BackgroundServiceBase`<T>`

The `BackgroundServiceBase<T>` class serves as a base for continuous long running background services that require enhanced features like custom logging and configurable service options.
It extends the ASP.NET Core's `BackgroundService` class, providing a more robust framework for handling background tasks.

This class is based on repeat intervals.

# BackgroundScheduleServiceBase`<T>`

The `BackgroundScheduleServiceBase<T>` class serves as a base for continuous long running background services that require enhanced features like custom logging and configurable service options.
It extends the ASP.NET Core's `BackgroundService` class, providing a more robust framework for handling background tasks.

This class is based on cron expression for scheduling.

- More information about cron expressions can be found on [wiki](https://en.wikipedia.org/wiki/Cron)
- To get help with defining a cron expression, use this [cron online helper](https://crontab.cronhub.io/)

## Cron format

Cron expression is a mask to define fixed times, dates and intervals. 
The mask consists of second (optional), minute, hour, day-of-month, month and day-of-week fields.
All of the fields allow you to specify multiple values, and any given date/time will satisfy the specified Cron expression, if all the fields contain a matching value.

```
                                       Allowed values    Allowed special characters   Comment

┌───────────── second (optional)       0-59              * , - /                      
│ ┌───────────── minute                0-59              * , - /                      
│ │ ┌───────────── hour                0-23              * , - /                      
│ │ │ ┌───────────── day of month      1-31              * , - / L W ?                
│ │ │ │ ┌───────────── month           1-12 or JAN-DEC   * , - /                      
│ │ │ │ │ ┌───────────── day of week   0-6  or SUN-SAT   * , - / # L ?                Both 0 and 7 means SUN
│ │ │ │ │ │
* * * * * *
```

## Features

### Logging

- Utilizes `ILogger<T>` for type-specific, high-performance logging.
- Automatically enriches log entries with the name of the service type (`T`).

### Error Handling

- Catches unhandled exceptions and logs them with a severity of `LogLevel.Warning`.
- Reruns the `DoWorkAsync` method after a configurable `repeat interval` for `BackgroundServiceBase` or `scheduled` for `BackgroundScheduleServiceBase`.
- For manual error handling hook into the exception handling in `DoWorkAsync` by overriding the `OnExceptionAsync` method.
- Designed to log errors rather than crashing the service.

### Configuration Options

- Allows for `startup delays` for `BackgroundServiceBase`.
- Configurable `repeat interval` for running tasks with `BackgroundServiceBase`.
- Configurable `cron expression` for scheduling running tasks with `BackgroundScheduleServiceBase`.

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

```csharp
public class MyBackgroundService : BackgroundScheduleServiceBase<MyBackgroundService>
{
    public MyBackgroundService(ILogger<MyBackgroundService> logger, IBackgroundScheduleServiceOptions options)
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

await host.RunAsync();
```

In this example the `TimeFileWorker` BackgroundService is wired up by using `AddHostedService<T>` as a normal `BackgroundService`.

Note: `TimeFileWorker` uses `TimeFileWorkerOptions` that implements `IBackgroundServiceOptions`.

## Setup BackgroundScheduleServiceBase via Dependency Injection

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
        services.Configure<TimeFileScheduleWorkerOptions>(configuration.GetSection(TimeFileScheduleWorkerOptions.SectionName));
        services.AddHostedService<TimeFileScheduleWorker>();
    })
    .Build();

await host.RunAsync();
```

In this example the `TimeFileScheduleWorker` BackgroundService is wired up by using `AddHostedService<T>` as a normal `BackgroundService`.

Note: `TimeFileScheduleWorker` uses `TimeFileScheduleWorkerOptions` that implements `IBackgroundScheduleServiceOptions`.

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

## Default implementation for `BackgroundServiceBase<>`
The `BackgroundServiceBase<>` automatically uses the `IBackgroundServiceHealthService` in the `DoWorkAsync` 'wait and retry' loop. This is archieved by providing the base constructor with a `IBackgroundServiceHealthService` instance.

```csharp
public TimeFileWorker(
    //...other parameters
    IBackgroundServiceHealthService healthService,
    //...other parameters
    )
    : base (healthService)
{
    //...other initializations
}
```

Now you not have to set the running state of the service in the `BackgroundService.StartAsync` and `BackgroundService.StopAsync` methods.

# Complete TimeFileWorker example

A sample reference implementation can be found in the sample project [`Atc.Hosting.TimeFile.Sample`](sample/Atc.Hosting.TimeFile.Sample/Program.cs)
which shows an example of the service `TimeFileWorker` that uses `BackgroundServiceBase` and the `IBackgroundServiceHealthService`.

```csharp
public class TimeFileWorker : BackgroundServiceBase<TimeFileWorker>
{
    private readonly ITimeProvider timeProvider;

    private readonly TimeFileWorkerOptions workerOptions;

    public TimeFileWorker(
        ILogger<TimeFileWorker> logger,
        IBackgroundServiceHealthService healthService,
        ITimeProvider timeProvider,
        IOptions<TimeFileWorkerOptions> workerOptions)
        : base(
            logger,
            workerOptions.Value,
            healthService)
    {
        this.timeProvider = timeProvider;
        this.workerOptions = workerOptions.Value;
    }

    public override Task StartAsync(
        CancellationToken cancellationToken)
    {
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(
        CancellationToken cancellationToken)
    {
        return base.StopAsync(cancellationToken);
    }

    public override Task DoWorkAsync(
        CancellationToken stoppingToken)
    {
        var isServiceRunning = healthService.IsServiceRunning(nameof(TimeFileWorker));

        Directory.CreateDirectory(workerOptions.OutputDirectory);

        var time = timeProvider.UtcNow;

        var outFile = Path.Combine(
            workerOptions.OutputDirectory,
            $"{nameof(TimeFileWorker)}.txt");

        return File.AppendAllLinesAsync(
            outFile,
            contents: [$"{time:yyyy-MM-dd HH:mm:ss} - {ServiceName} - IsRunning={isServiceRunning}"],
            stoppingToken);
    }

    protected override Task OnExceptionAsync(
        Exception exception,
        CancellationToken stoppingToken)
    {
        if (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogCritical(exception, "Could not write file!");
            return StopAsync(stoppingToken);
        }

        return base.OnExceptionAsync(exception, stoppingToken);
    }
}
```

# Complete TimeFileScheduleWorker example

A sample reference implementation can be found in the sample project [`Atc.Hosting.TimeFile.Sample`](sample/Atc.Hosting.TimeFile.Sample/Program.cs)
which shows an example of the service `TimeFileScheduleWorker` that uses `BackgroundScheduleServiceBase` and the `IBackgroundServiceHealthService`.

```csharp
public class TimeFileScheduleWorker : BackgroundScheduleServiceBase<TimeFileScheduleWorker>
{
    private readonly ITimeProvider timeProvider;

    private readonly TimeFileScheduleWorkerOptions workerOptions;

    public TimeFileWorker(
        ILogger<TimeFileScheduleWorker> logger,
        IBackgroundServiceHealthService healthService,
        ITimeProvider timeProvider,
        IOptions<TimeFileScheduleWorkerOptions> workerOptions)
        : base(
            logger,
            workerOptions.Value,
            healthService)
    {
        this.timeProvider = timeProvider;
        this.workerOptions = workerOptions.Value;
    }

    public override Task StartAsync(
        CancellationToken cancellationToken)
    {
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(
        CancellationToken cancellationToken)
    {
        return base.StopAsync(cancellationToken);
    }

    public override Task DoWorkAsync(
        CancellationToken stoppingToken)
    {
        var isServiceRunning = healthService.IsServiceRunning(nameof(TimeFileWorker));

        Directory.CreateDirectory(workerOptions.OutputDirectory);

        var time = timeProvider.UtcNow;

        var outFile = Path.Combine(
            workerOptions.OutputDirectory,
            $"{nameof(TimeFileScheduleWorker)}.txt");

        return File.AppendAllLinesAsync(
            outFile,
            contents: [$"{time:yyyy-MM-dd HH:mm:ss} - {ServiceName} - IsRunning={isServiceRunning}"],
            stoppingToken);
    }

    protected override Task OnExceptionAsync(
        Exception exception,
        CancellationToken stoppingToken)
    {
        if (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogCritical(exception, "Could not write file!");
            return StopAsync(stoppingToken);
        }

        return base.OnExceptionAsync(exception, stoppingToken);
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

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

# How to contribute

[Contribution Guidelines](https://atc-net.github.io/introduction/about-atc#how-to-contribute)

[Coding Guidelines](https://atc-net.github.io/introduction/about-atc#coding-guidelines)
