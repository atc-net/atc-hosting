// ReSharper disable StringLiteralTypo
using Atc.Hosting.TimeFile.Sample;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var host = Host
    .CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.Configure<TimeFileWorkerOptions>(configuration.GetSection(TimeFileWorkerOptions.SectionName));
        services.AddHostedService<TimeFileWorker>();
        services.AddSingleton<ITimeService, TimeService>();
    })
    .Build();

host.Run();