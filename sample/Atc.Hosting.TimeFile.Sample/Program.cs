// ReSharper disable StringLiteralTypo
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var host = Host
    .CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton(TimeProvider.System);

        // Registers the options, the hosted service and its health wiring - or only the options when "Enabled" is false.
        services.AddBackgroundService<TimeFileWorker, TimeFileWorkerOptions>(
            configuration.GetSection(TimeFileWorkerOptions.SectionName));

        services.AddBackgroundScheduleService<TimeFileScheduleWorker, TimeFileScheduleWorkerOptions>(
            configuration.GetSection(TimeFileScheduleWorkerOptions.SectionName));
    })
    .Build();

await host.RunAsync();