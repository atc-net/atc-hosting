namespace Atc.Hosting.Tests.XUnitTestTypes;

/// <summary>
/// A second options type, for tests that register two services (each background service needs its own options type).
/// </summary>
public class MyOtherServiceOptions : MyServiceOptions
{
    public string OtherSetting { get; set; } = "other";
}