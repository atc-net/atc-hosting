namespace Atc.Hosting.TimeFile.Sample;

public class TimeFileScheduleWorkerOptions : IBackgroundScheduleServiceOptions
{
    public const string SectionName = "TimeFileScheduleWorker";

    public string OutputDirectory { get; set; } = Path.GetTempPath();

    public string CronExpression { get; set; } = "*/5 * * * *";

    public string? ServiceName { get; set; }

    public override string ToString()
        => $"{nameof(OutputDirectory)}: {OutputDirectory}, {nameof(CronExpression)}: {CronExpression}, {nameof(ServiceName)}: {ServiceName}";
}