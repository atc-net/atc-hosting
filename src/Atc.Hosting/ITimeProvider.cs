namespace Atc.Hosting;

public interface ITimeProvider
{
    DateTime UtcNow { get; }
}