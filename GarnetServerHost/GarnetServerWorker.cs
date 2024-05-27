using System;
using System.Threading;
using System.Threading.Tasks;
using Garnet;
using Garnet.server;
using GarnetServerHost.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GarnetServerHost;

public class GarnetServerWorker : BackgroundService
{
    private readonly ILogger<GarnetServerWorker> _logger;
    private readonly ILoggerFactory _loggerFactory;
    
    private readonly GarnetServerOptions _garnetServerOptions;

    public GarnetServerWorker(
        ILogger<GarnetServerWorker> logger,
        ILoggerFactory loggerFactory,
        IOptions<GarnetServerOptions> garnetServerOptions
    )
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _garnetServerOptions = garnetServerOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var server = new GarnetServer(
                opts: _garnetServerOptions,
                loggerFactory: _loggerFactory
            );
            server.Start();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.UnableToInitializeGarnetServer(ex);
        }
    }
}
