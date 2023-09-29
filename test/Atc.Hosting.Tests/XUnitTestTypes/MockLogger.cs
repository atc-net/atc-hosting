namespace Atc.Hosting.Tests.XUnitTestTypes;

public abstract class MockLogger<T> : ILogger<T>
{
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
        => Log(
            logLevel,
            formatter(state, exception),
            exception);

    public abstract void Log(
        LogLevel logLevel,
        object state,
        Exception? exception = null);

    public abstract bool IsEnabled(LogLevel logLevel);

    public abstract IDisposable? BeginScope<TState>(TState state);
}