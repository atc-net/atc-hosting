namespace Atc.Hosting;

public class SystemTimeProvider : ITimeProvider
{
    public DateTime UtcNow
        => DateTime.UtcNow;
}