using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Garnet.server;
using GarnetServerHost.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace GarnetServerHost;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog((HostBuilderContext hostingContext, IServiceProvider services, LoggerConfiguration loggerConfiguration) =>
            {
                loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration);

                loggerConfiguration
                    .Enrich.FromLogContext();
            })
            .ConfigureAppConfiguration((builderContext, configBuilder) =>
            {
                var config = configBuilder.Build();

                // add the Docker secrets
                configBuilder.AddKeyPerFile(source =>
                {
                    // get the secrets file directory
                    var secretsOptions = new DockerSecretsConfigSourceOptions();
                    config.GetSection(DockerSecretsConfigSourceOptions.ConfigBinding).Bind(secretsOptions);

                    source.Optional = true;

                    if (
                        !string.IsNullOrWhiteSpace(secretsOptions?.RootFolder)
                    && Directory.Exists(secretsOptions.RootFolder)
                    )
                    {
                        // root folder has been provided, use this provider
                        source.ReloadOnChange = true;
                        source.FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(secretsOptions.RootFolder, Microsoft.Extensions.FileProviders.Physical.ExclusionFilters.None);
                    }
                });

            })
            .ConfigureServices((hostContext, services) =>
            {
                var configuration = hostContext.Configuration;

                services.AddSingleton<IOptions<GarnetServerOptions>>(services =>
                {
                    var garnetOptions = CreateServerOptionsFromConfig(configuration, services);
                    return Microsoft.Extensions.Options.Options.Create(garnetOptions);
                });

                services.AddHostedService<GarnetServerWorker>();
            });

    private static GarnetServerOptions CreateServerOptionsFromConfig(IConfiguration configuration, IServiceProvider services)
    {
        // the GarnetServerOptions has public fields but not properties (no setter or getters)
        // instead, we can set these values using reflection since Bind does not support this
        var logger = services.GetService<Microsoft.Extensions.Logging.ILogger>();
        var garnetOptions = new GarnetServerOptions(logger: logger);
        var section = configuration.GetSection("Garnet");

        // use Bind to set the options that are supported, i.e., the Public Properties
        section.Bind(garnetOptions);

        var props =
            garnetOptions.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var property in props)
        {
            string configOption = section[property.Name];
            if (!string.IsNullOrWhiteSpace(configOption))
            {
                property.SetValue(garnetOptions, configOption);
            }
        }

        return garnetOptions;
    }
}