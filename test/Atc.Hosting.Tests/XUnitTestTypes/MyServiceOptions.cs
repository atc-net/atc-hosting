namespace Atc.Hosting.Tests.XUnitTestTypes;

public class MyServiceOptions : IBackgroundServiceOptions
{
    public bool Enabled { get; set; } = true;

    public ushort StartupDelaySeconds { get; set; } = 1;

    public ushort RepeatIntervalSeconds { get; set; } = 1;

    public ushort? MaxWorkDurationSeconds { get; set; }

    public ushort? MaxConsecutiveFailures { get; set; }

    public string? ServiceName { get; set; }

    public override string ToString()
        => $"{nameof(Enabled)}: {Enabled}, {nameof(StartupDelaySeconds)}: {StartupDelaySeconds}, {nameof(RepeatIntervalSeconds)}: {RepeatIntervalSeconds}, {nameof(MaxWorkDurationSeconds)}: {MaxWorkDurationSeconds}, {nameof(MaxConsecutiveFailures)}: {MaxConsecutiveFailures}, {nameof(ServiceName)}: {ServiceName}";
}