namespace Atc.Hosting.Tests.XUnitTestTypes;

public class MyWorkerException : Exception
{
    public MyWorkerException()
    {
    }

    public MyWorkerException(string message)
        : base(message)
    {
    }

    public MyWorkerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}