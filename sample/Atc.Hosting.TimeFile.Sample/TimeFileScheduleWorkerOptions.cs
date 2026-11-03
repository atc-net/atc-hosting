namespace Atc.Hosting.TimeFile.Sample;

public class TimeFileScheduleWorkerOptions : IBackgroundScheduleServiceOptions
{
    public const string SectionName = "TimeFileScheduleWorker";

    public bool Enabled { get; set; } = true;

    public string OutputDirectory { get; set; } = Path.GetTempPath();

    public string CronExpression { get; set; } = "*/5 * * * *";

    public ushort? MaxWorkDurationSeconds { get; set; }

    public ushort? MaxConsecutiveFailures { get; set; }

    public string? ServiceName { get; set; }

    public override string ToString()
        => $"{nameof(Enabled)}: {Enabled}, {nameof(OutputDirectory)}: {OutputDirectory}, {nameof(CronExpression)}: {CronExpression}, {nameof(MaxWorkDurationSeconds)}: {MaxWorkDurationSeconds}, {nameof(MaxConsecutiveFailures)}: {MaxConsecutiveFailures}, {nameof(ServiceName)}: {ServiceName}";
}