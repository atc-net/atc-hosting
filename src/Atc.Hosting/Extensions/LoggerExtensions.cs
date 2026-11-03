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
        EventId = LoggingEventIdConstants.BackgroundService.Disabled,
        Level = LogLevel.Information,
        Message = "Background service '{ServiceName}' is disabled and will not run",
        SkipEnabledCheck = false)]
    internal static partial void LogBackgroundServiceDisabled(
        this ILogger logger,
        string serviceName);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundService.WorkExceededMaxDuration,
        Level = LogLevel.Warning,
        Message = "Worker {ServiceName} has been running for more than {MaxWorkDurationSeconds} seconds and is reported as not running until the run completes",
        SkipEnabledCheck = false)]
    internal static partial void LogBackgroundServiceWorkExceededMaxDuration(
        this ILogger logger,
        string serviceName,
        int maxWorkDurationSeconds);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundService.ConsecutiveFailures,
        Level = LogLevel.Warning,
        Message = "Worker {ServiceName} has failed {FailureCount} times in a row and is reported as not running until a run succeeds",
        SkipEnabledCheck = false)]
    internal static partial void LogBackgroundServiceConsecutiveFailures(
        this ILogger logger,
        string serviceName,
        int failureCount);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundService.StopTimedOut,
        Level = LogLevel.Warning,
        Message = "Worker {ServiceName} did not stop within the shutdown timeout. Make sure DoWorkAsync observes the stopping token",
        SkipEnabledCheck = false)]
    internal static partial void LogBackgroundServiceStopTimedOut(
        this ILogger logger,
        string serviceName);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundService.Stopped,
        Level = LogLevel.Information,
        Message = "Stopped worker {ServiceName}",
        SkipEnabledCheck = false)]
    internal static partial void LogBackgroundServiceStopped(
        this ILogger logger,
        string serviceName);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundService.Stopping,
        Level = LogLevel.Warning,
        Message = "No next occurrence found for cron expression '{CronExpression}'. Service {ServiceName} will stop",
        SkipEnabledCheck = false)]
    internal static partial void LogBackgroundServiceStopping(
        this ILogger logger,
        string cronExpression,
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
        Message = "Worker {ServiceName} will retry on next occurrence per the cron expression {CronExpression}",
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