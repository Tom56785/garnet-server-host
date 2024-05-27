using Garnet.server;
using Microsoft.Extensions.Logging;

namespace GarnetServerHost.Options;

public class ExtendedGarnetServerOptions : GarnetServerOptions
{
    public const string ConfigBinding = "Garnet";
    public string Password { get; set; }

    public ExtendedGarnetServerOptions(ILogger logger = null)
        : base(logger)
    {
        base.logger = logger;
    }
}