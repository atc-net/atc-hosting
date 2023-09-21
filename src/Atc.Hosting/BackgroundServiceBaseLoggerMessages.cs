// ReSharper disable InconsistentNaming
namespace Atc.Hosting;

/// <summary>
/// BackgroundServiceBaseLoggerMessages
/// </summary>
internal static partial class BackgroundServiceBaseLoggerMessages
{
    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundServiceStarted,
        Level = LogLevel.Information,
        Message = "Starting worker {serviceName}. Worker will run with {repeatIntervalSeconds} seconds interval",
        SkipEnabledCheck = false)]
    internal static partial void LogBackgroundServiceStarted(
        this ILogger logger,
        string serviceName,
        int repeatIntervalSeconds);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundServiceStopped,
        Level = LogLevel.Information,
        Message = "Execution ended for worker {serviceName}. Cancellation token cancelled = {isCancellationRequested}",
        SkipEnabledCheck = false)]
    internal static partial void LogBackgroundServiceStopped(
        this ILogger logger,
        string serviceName,
        bool isCancellationRequested);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundServiceCancelled,
        Level = LogLevel.Warning,
        Message = "Execution cancelled on worker {serviceName}",
        SkipEnabledCheck = false)]
    internal static partial void LogBackgroundServiceCancelled(
        this ILogger logger,
        string serviceName);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundServiceRetrying,
        Level = LogLevel.Warning,
        Message = "Unhandled exception occurred in worker {serviceName}. Worker will retry after {repeatIntervalSeconds} seconds",
        SkipEnabledCheck = false)]
    internal static partial void LogBackgroundServiceRetrying(
        this ILogger logger,
        string serviceName,
        int repeatIntervalSeconds,
        Exception exception);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundServiceUnhandledException,
        Level = LogLevel.Error,
        Message = "Unhandled exception occurred in worker {serviceName}",
        SkipEnabledCheck = false)]
    internal static partial void LogBackgroundServiceUnhandledException(
        this ILogger logger,
        string serviceName,
        Exception exception);
}