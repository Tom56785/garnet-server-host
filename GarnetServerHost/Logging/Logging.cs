using System;
using Microsoft.Extensions.Logging;

namespace GarnetServerHost.Logging;

public static partial class Logging
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Error,
        Message = "Unable to initialize the Garnet server!")]
    public static partial void UnableToInitializeGarnetServer(this ILogger logger, Exception exception);
}