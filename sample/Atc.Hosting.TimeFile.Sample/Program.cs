// ReSharper disable StringLiteralTypo
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var host = Host
    .CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<ITimeProvider, SystemTimeProvider>();
        services.Configure<TimeFileWorkerOptions>(configuration.GetSection(TimeFileWorkerOptions.SectionName));
        services.AddHostedService<TimeFileWorker>();

        services.AddSingleton<IBackgroundServiceHealthService, BackgroundServiceHealthService>(s =>
        {
            var healthService = new BackgroundServiceHealthService(s.GetRequiredService<ITimeProvider>());

            var timeFileWorkerOptions = s.GetRequiredService<IOptions<TimeFileWorkerOptions>>().Value;
            healthService.SetMaxStalenessInSeconds(nameof(TimeFileWorker), timeFileWorkerOptions.RepeatIntervalSeconds);

            return healthService;
        });
    })
    .Build();

host.Run();