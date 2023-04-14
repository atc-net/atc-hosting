// ReSharper disable CheckNamespace
namespace Atc.Hosting;

public static class ServiceProviderExtensions
{
    /// <summary>
    /// Gets the hosted worker background service.
    /// </summary>
    /// <typeparam name="TWorkerType">The type of the worker background service type.</typeparam>
    /// <param name="serviceProvider">The service provider.</param>
    /// <example>
    /// <![CDATA[
    /// // In startup.cs under ConfigureServices - add:
    /// services.AddHostedService<MyBackgroundService>();
    ///
    /// // In a MVC controller method, the following can extract the singleton service:
    /// var myBackgroundService = HttpContext.RequestServices.GetHostedService<MyBackgroundService>();
    /// ]]>
    /// </example>
    public static TWorkerType? GetHostedService<TWorkerType>(
        this IServiceProvider serviceProvider)
        => serviceProvider
            .GetServices<IHostedService>()
            .OfType<TWorkerType>()
            .FirstOrDefault();
}