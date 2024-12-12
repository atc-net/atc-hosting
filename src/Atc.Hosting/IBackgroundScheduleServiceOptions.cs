namespace Atc.Hosting;

/// <summary>
/// Defines the options for the background schedule service.
/// </summary>
/// <remarks>
/// Need help with cron expression - https://crontab.cronhub.io/
/// <list type="table">
/// <listheader>
///     <term>Cron Expression examples</term>
///     <term>Description</term>
/// </listheader>
/// <item>
///     <term>*/5 * * * *</term>
///     <term>Every 5 minutes</term>
/// </item>
/// <item>
///     <term>0 0 * * *</term>
///     <term>Every day at midnight</term>
/// </item>
/// <item>
///     <term>0 12 * * MON</term>
///     <term>Every Monday at noon</term>
/// </item>
/// <item>
///     <term>0 0 1 * *</term>
///     <term>First day of every month at midnight</term>
/// </item>
/// <item>
///     <term>0 0 * * SUN</term>
///     <term>Every Sunday at midnight</term>
/// </item>
/// <item>
///     <term>0 15 10 * *</term>
///     <term>Every day at 10:15 AM</term>
/// </item>
/// </list>
/// </remarks>
public interface IBackgroundScheduleServiceOptions : IBackgroundServiceBaseOptions
{
    /// <summary>
    /// Gets the cron expression for scheduling.
    /// </summary>
    string CronExpression { get; }
}