namespace Atc.Hosting.Tests.XUnitTestTypes;

public class MyServiceOptions : IBackgroundServiceOptions
{
    public ushort StartupDelaySeconds { get; set; } = 1;

    public ushort RepeatIntervalSeconds { get; set; } = 1;

    public override string ToString()
        => $"{nameof(StartupDelaySeconds)}: {StartupDelaySeconds}, {nameof(RepeatIntervalSeconds)}: {RepeatIntervalSeconds}";
}