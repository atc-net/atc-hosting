namespace Atc.Hosting.TimeFile.Sample;

public class TimeFileWorkerOptions : IBackgroundServiceOptions
{
    public const string SectionName = "TimeFileWorker";

    public string OutputDirectory { get; set; } = Path.GetTempPath();

    public ushort StartupDelaySeconds { get; set; } = 1;

    public ushort RetryCount { get; set; } = 3;

    public ushort RepeatIntervalSeconds { get; set; } = 20;

    public override string ToString()
        => $"{nameof(OutputDirectory)}: {OutputDirectory}, {nameof(StartupDelaySeconds)}: {StartupDelaySeconds}, {nameof(RetryCount)}: {RetryCount}, {nameof(RepeatIntervalSeconds)}: {RepeatIntervalSeconds}";
}