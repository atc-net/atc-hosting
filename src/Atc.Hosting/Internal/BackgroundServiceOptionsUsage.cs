namespace Atc.Hosting.Internal;

/// <summary>
/// Records that an options type is used by a background service registered through <c>AddBackgroundService</c>
/// or <c>AddBackgroundScheduleService</c>, including disabled ones, because their options are registered too.
/// </summary>
/// <param name="OptionsType">The options type.</param>
/// <param name="ServiceName">The name of the service that uses it.</param>
internal sealed record BackgroundServiceOptionsUsage(
    Type OptionsType,
    string ServiceName);