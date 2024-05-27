using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Garnet.server;
using GarnetServerHost.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;

namespace GarnetServerHost;

public class Program
{
    private const string GarnetOptionsConfigBinding = "Garnet";

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
                    var logger = services.GetService<Microsoft.Extensions.Logging.ILogger>();
                    var garnetOptions = new GarnetServerOptions(logger: logger);
                    var section = configuration.GetSection(GarnetOptionsConfigBinding);

                    // serialize the configuration to JSON, then we'll deserialize into the GarnetServerOptions manually
                    var configJsonNode = Serialize(section);

                    if (configJsonNode != null)
                    {
                        var configJson = configJsonNode.ToJsonString();

                        // we have to use Newtonsoft instead of System.Text.Json as it supports public fields
                        JsonConvert.PopulateObject(configJson, garnetOptions);
                    }

                    return Microsoft.Extensions.Options.Options.Create(garnetOptions);
                });

                services.AddHostedService<GarnetServerWorker>();
            });

    private static JsonNode Serialize(IConfiguration config)
    {
        JsonObject obj = new();

        foreach (var child in config.GetChildren())
        {
            if (child.Path.EndsWith(":0", StringComparison.OrdinalIgnoreCase))
            {
                var arr = new JsonArray();

                foreach (var arrayChild in config.GetChildren())
                {
                    arr.Add(Serialize(arrayChild));
                }

                return arr;
            }
            else
            {
                obj.Add(child.Key, Serialize(child));
            }
        }

        if (obj.Count == 0 && config is IConfigurationSection section)
        {
            if (bool.TryParse(section.Value, out bool boolean))
            {
                return JsonValue.Create(boolean);
            }
            else if (decimal.TryParse(section.Value, out decimal real))
            {
                return JsonValue.Create(real);
            }
            else if (long.TryParse(section.Value, out long integer))
            {
                return JsonValue.Create(integer);
            }

            return JsonValue.Create(section.Value);
        }

        return obj;
    }
}