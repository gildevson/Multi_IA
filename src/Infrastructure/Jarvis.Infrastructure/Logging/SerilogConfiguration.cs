using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace Jarvis.Infrastructure.Logging;

public static class SerilogConfiguration
{
    public static LoggerConfiguration Configure(LoggerConfiguration config, IConfiguration configuration, string logsFolder = "logs")
    {
        Directory.CreateDirectory(logsFolder);

        return config
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "JARVIS")
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: Path.Combine(logsFolder, "jarvis-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
    }
}