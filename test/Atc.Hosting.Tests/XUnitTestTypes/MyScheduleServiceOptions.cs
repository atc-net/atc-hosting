namespace Atc.Hosting.Tests.XUnitTestTypes;

public class MyScheduleServiceOptions : IBackgroundScheduleServiceOptions
{
    public bool Enabled { get; set; } = true;

    public string CronExpression { get; set; } = "* * * * *";

    public ushort? MaxWorkDurationSeconds { get; set; }

    public string? ServiceName { get; set; }

    public override string ToString()
        => $"{nameof(Enabled)}: {Enabled}, {nameof(CronExpression)}: {CronExpression}, {nameof(MaxWorkDurationSeconds)}: {MaxWorkDurationSeconds}, {nameof(ServiceName)}: {ServiceName}";
}