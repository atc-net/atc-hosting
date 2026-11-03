namespace Atc.Hosting.TimeFile.Sample;

public class TimeFileWorkerOptions : IBackgroundServiceOptions
{
    public const string SectionName = "TimeFileWorker";

    public bool Enabled { get; set; } = true;

    public string OutputDirectory { get; set; } = Path.GetTempPath();

    public ushort StartupDelaySeconds { get; set; } = 1;

    public ushort RepeatIntervalSeconds { get; set; } = 20;

    public ushort? MaxWorkDurationSeconds { get; set; }

    public ushort? MaxConsecutiveFailures { get; set; }

    public string? ServiceName { get; set; }

    public override string ToString()
        => $"{nameof(Enabled)}: {Enabled}, {nameof(OutputDirectory)}: {OutputDirectory}, {nameof(StartupDelaySeconds)}: {StartupDelaySeconds}, {nameof(RepeatIntervalSeconds)}: {RepeatIntervalSeconds}, {nameof(MaxWorkDurationSeconds)}: {MaxWorkDurationSeconds}, {nameof(MaxConsecutiveFailures)}: {MaxConsecutiveFailures}, {nameof(ServiceName)}: {ServiceName}";
}