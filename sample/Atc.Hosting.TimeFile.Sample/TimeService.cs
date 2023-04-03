namespace Atc.Hosting.TimeFile.Sample;

public class TimeService : ITimeService
{
    public DateTime GetDateTime()
        => DateTime.Now;
}