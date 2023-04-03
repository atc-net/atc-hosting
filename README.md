[![NuGet Version](https://img.shields.io/nuget/v/atc.hosting.svg?logo=nuget&style=for-the-badge)](https://www.nuget.org/packages/atc.hosting)

# Atc.Hosting

Contains various classes and extensions methods for the hosting namespace, e.g. BackgroundServices

## Why BackgroundServiceBase < T > instead of BackgroundService

`BackgroundServiceBase` extend the `BackgroundService` with features like:

* High-performance logging by using of `ILogger`
* Retries on DoWorkAsync by using of `Polly`
* Error handling using logging instead of service cash
* Configuration for:
    * Startup-Delay
    * Retry-Count
    * Repeat-Interval
* Simple to use

## Using BackgroundServiceBase < T >

A sample reference implementation can be found in the sample project [`Atc.Hosting.TimeFile.Sample`](sample/Atc.Hosting.TimeFile.Sample/Program.cs)
and it shows an example of the service `TimeFileWorker` that use `BackgroundServiceBase`.

### Program.cs

In `Program.cs` the `TimeFileWorker` is wired up by using `AddHostedService` as a normal `BackgroundService`.

Note: `TimeFileWorker` use a `TimeFileWorkerOptions` that implement `IBackgroundServiceOptions`.


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

### TimeFileWorker.cs

In the example of the service `TimeFileWorker` that use `BackgroundServiceBase` 
shows a simple example of the only method `DoWorkAsync` that have to be implemented.

```csharp
public class TimeFileWorker : BackgroundServiceBase<TimeFileWorker>
{
    private readonly TimeFileWorkerOptions workerOptions;
    private readonly ITimeService timeService;

    public TimeFileWorker(
        ILogger<TimeFileWorker> logger,
        IOptions<TimeFileWorkerOptions> workerOptions,
        ITimeService timeService)
        : base(
            logger,
            workerOptions.Value)
    {
        this.workerOptions = workerOptions.Value;
        this.timeService = timeService;
    }

    public override Task DoWorkAsync(
        CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(workerOptions.OutputDirectory);

        var time = timeService.GetDateTime();

        var outFile = Path.Combine(
            workerOptions.OutputDirectory,
            $"{time:yyyy-MM-dd--HHmmss}.txt");

        return File.WriteAllTextAsync(outFile, ServiceName, stoppingToken);
    }
}
```

## Requirements

* [.NET 7 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)

## How to contribute

[Contribution Guidelines](https://atc-net.github.io/introduction/about-atc#how-to-contribute)

[Coding Guidelines](https://atc-net.github.io/introduction/about-atc#coding-guidelines)
