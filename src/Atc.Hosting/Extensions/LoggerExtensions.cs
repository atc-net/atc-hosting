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
        Message = "Started worker {serviceName}. Worker will run with {repeatIntervalSeconds} seconds interval",
        SkipEnabledCheck = false)]
    internal static partial void LogBackgroundServiceStarted(
        this ILogger logger,
        string serviceName,
        int repeatIntervalSeconds);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundService.Started,
        Level = LogLevel.Information,
        Message = "Started worker {serviceName}. Worker will run with cron expression {cronExpression}",
        SkipEnabledCheck = false)]
    internal static partial void LogBackgroundServiceStarted(
        this ILogger logger,
        string serviceName,
        string cronExpression);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundService.Stopped,
        Level = LogLevel.Information,
        Message = "Stopped worker {serviceName}",
        SkipEnabledCheck = false)]
    internal static partial void LogBackgroundServiceStopped(
        this ILogger logger,
        string serviceName);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundService.Cancelled,
        Level = LogLevel.Warning,
        Message = "Cancellation invoked for worker {serviceName}",
        SkipEnabledCheck = false)]
    internal static partial void LogBackgroundServiceCancelled(
        this ILogger logger,
        string serviceName);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundService.Retrying,
        Level = LogLevel.Warning,
        Message = "Unhandled exception occurred in worker {serviceName}. Worker will retry after {repeatIntervalSeconds} seconds",
        SkipEnabledCheck = false)]
    internal static partial void LogBackgroundServiceRetrying(
        this ILogger logger,
        string serviceName,
        int repeatIntervalSeconds,
        Exception exception);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundService.Retrying,
        Level = LogLevel.Warning,
        Message = "Unhandled exception occurred in worker {serviceName}. Worker will retry on next defined in cron expression {cronExpression}",
        SkipEnabledCheck = false)]
    internal static partial void LogBackgroundServiceRetrying(
        this ILogger logger,
        string serviceName,
        string cronExpression,
        Exception exception);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundService.UnhandledException,
        Level = LogLevel.Error,
        Message = "Unhandled exception occurred in worker {serviceName}",
        SkipEnabledCheck = false)]
    internal static partial void LogBackgroundServiceUnhandledException(
        this ILogger logger,
        string serviceName,
        Exception exception);
}