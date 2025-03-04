// ReSharper disable CheckNamespace
namespace Atc.Hosting;

/// <summary>
/// LoggerExtensions for source generated logging.
/// </summary>
internal static partial class LoggerExtensions
{
    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundService.Started,
        Level = LogLevel.Information,
        Message = "Started worker {ServiceName}. Worker will run with {RepeatIntervalSeconds} seconds interval",
        SkipEnabledCheck = false)]
    internal static partial void LogBackgroundServiceStarted(
        this ILogger logger,
        string serviceName,
        int repeatIntervalSeconds);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundService.Started,
        Level = LogLevel.Information,
        Message = "Started worker {ServiceName}. Worker will run with cron expression {CronExpression}",
        SkipEnabledCheck = false)]
    internal static partial void LogBackgroundServiceStarted(
        this ILogger logger,
        string serviceName,
        string cronExpression);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundService.Stopped,
        Level = LogLevel.Information,
        Message = "Stopped worker {ServiceName}",
        SkipEnabledCheck = false)]
    internal static partial void LogBackgroundServiceStopped(
        this ILogger logger,
        string serviceName);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundService.Cancelled,
        Level = LogLevel.Warning,
        Message = "Cancellation invoked for worker {ServiceName}",
        SkipEnabledCheck = false)]
    internal static partial void LogBackgroundServiceCancelled(
        this ILogger logger,
        string serviceName);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundService.Retrying,
        Level = LogLevel.Information,
        Message = "Worker {ServiceName} will retry after {RepeatIntervalSeconds} seconds",
        SkipEnabledCheck = false)]
    internal static partial void LogBackgroundServiceRetrying(
        this ILogger logger,
        string serviceName,
        int repeatIntervalSeconds);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundService.Retrying,
        Level = LogLevel.Information,
        Message = "Worker {ServiceName} will retry on next occurence pr the cron expression {CronExpression}",
        SkipEnabledCheck = false)]
    internal static partial void LogBackgroundServiceRetrying(
        this ILogger logger,
        string serviceName,
        string cronExpression);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundService.UnhandledException,
        Level = LogLevel.Error,
        Message = "Unhandled exception occurred in worker {ServiceName}",
        SkipEnabledCheck = false)]
    internal static partial void LogBackgroundServiceUnhandledException(
        this ILogger logger,
        Exception exception,
        string serviceName);
}