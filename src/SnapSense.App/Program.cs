using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SnapSense;

internal static class Program
{
    private static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((_, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
            })
            .ConfigureServices((builderContext, services) =>
            {
                services
                    .Configure<FacePersistenceFrameHandlerOptions>(
                        builderContext.Configuration.GetSection(
                            FacePersistenceFrameHandlerOptions.ConfigurationSection));

                services.AddSingleton<FaceDetector>();
                services.AddSingleton<IFrameHandler, FacePersistenceFrameHandler>();
                services.AddSingleton<WebcamHandler>();
                services.AddHostedService<WebcamHostedService>();
            })
            .ConfigureLogging((_, logging) =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddConsole();
            });
}
