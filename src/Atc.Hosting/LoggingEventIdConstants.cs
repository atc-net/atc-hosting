namespace Atc.Hosting;

public static class LoggingEventIdConstants
{
    internal static class BackgroundService
    {
        public const int Started = 20000;
        public const int Retrying = 20001;
        public const int Stopped = 20002;
        public const int Cancelled = 20003;
        public const int UnhandledException = 20004;
        public const int Stopping = 20005;
        public const int Disabled = 20006;
        public const int WorkExceededMaxDuration = 20007;
        public const int StopTimedOut = 20008;
        public const int ConsecutiveFailures = 20009;
    }
}