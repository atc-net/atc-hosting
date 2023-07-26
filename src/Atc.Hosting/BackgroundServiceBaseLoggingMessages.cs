// ReSharper disable InconsistentNaming
namespace Atc.Hosting;

/// <summary>
/// BackgroundServiceBase - Logging.
/// </summary>
[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK.")]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "OK")]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1619:GenericTypeParametersMustBeDocumentedPartialClass", Justification = "OK.")]
public partial class BackgroundServiceBase<T>
{
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "OK.")]
    [SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "OK.")]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "OK.")]
    protected readonly ILogger logger;

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundServiceStarted,
        Level = LogLevel.Information,
        Message = "Starting {serviceName}. Runs every {repeatIntervalSeconds} seconds")]
    private partial void LogBackgroundServiceStarted(
        string serviceName,
        int repeatIntervalSeconds);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundServiceStopped,
        Level = LogLevel.Information,
        Message = "Execution ended for worker {serviceName}. Cancellation token cancelled = {isCancellationRequested}")]
    private partial void LogBackgroundServiceStopped(
        string serviceName,
        bool isCancellationRequested);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundServiceCancelled,
        Level = LogLevel.Warning,
        Message = "Execution cancelled on worker {serviceName}")]
    private partial void LogBackgroundServiceCancelled(
        string serviceName);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundServiceRetrying,
        Level = LogLevel.Warning,
        Message = "Unhandled exception occurred in worker {serviceName}. Worker will retry after {repeatIntervalSeconds} seconds - error: '{errorMessage}'")]
    private partial void LogBackgroundServiceRetrying(
        string serviceName,
        int repeatIntervalSeconds,
        string errorMessage);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BackgroundServiceUnhandledException,
        Level = LogLevel.Error,
        Message = "Unhandled exception. Execution Stopping for worker {serviceName} - error: '{errorMessage}'")]
    private partial void LogBackgroundServiceUnhandledException(
        string serviceName,
        string errorMessage);
}