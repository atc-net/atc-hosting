[![NuGet Version](https://img.shields.io/nuget/v/atc.hosting.healthchecks.svg?logo=nuget&style=for-the-badge)](https://www.nuget.org/packages/atc.hosting.healthchecks)

# Atc.Hosting.HealthChecks

Health checks for background services built on [Atc.Hosting](https://www.nuget.org/packages/atc.hosting). It adds one health check per background service, named after the service, which reports whether the service is running.

## Getting started

Register your background services with `AddBackgroundService` or `AddBackgroundScheduleService` from Atc.Hosting, then add the health checks:

```csharp
using Atc.Hosting.HealthChecks;

services.AddBackgroundService<TimeFileWorker, TimeFileWorkerOptions>(
    configuration.GetSection(TimeFileWorkerOptions.SectionName));

services
    .AddHealthChecks()
    .AddBackgroundServices();
```

This adds one check named `TimeFileWorker`. The services are read when the health checks are first used, so `AddBackgroundServices()` can come before or after the registrations. Disabled services get no check.

Each check is `Healthy` while the service is running, and the failure status otherwise. The descriptions are `Background service '{name}' is healthy` and `Background service '{name}' is unhealthy.`

## Options

All parameters are optional:

```csharp
services
    .AddHealthChecks()
    .AddBackgroundServices(
        failureStatus: HealthStatus.Degraded,  // default: Unhealthy
        tags: ["dependency"],
        timeout: TimeSpan.FromSeconds(5));
```

## Services registered with `AddHostedService`

The parameterless overload only knows services registered through Atc.Hosting's extensions. For services registered with plain `AddHostedService<T>()`, pass their names:

```csharp
services.AddHostedService<OldWorker>();
services.AddBackgroundServiceHealthService((nameof(OldWorker), 30));

services
    .AddHealthChecks()
    .AddBackgroundServices([nameof(OldWorker)]);
```

`AddBackgroundServiceHealthService` registers the health service the checks read from, and the worker's max staleness in seconds.

## Documentation

See the [Atc.Hosting README](https://github.com/atc-net/atc-hosting#readme) for background services, the health service and its heartbeat.