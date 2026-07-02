using Serilog;
using Serilog.Events;

namespace LudoProjects.Helper;

internal abstract class Logger
{
    public static void ConfigureLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "LudoProjects")
            .WriteTo.Console(
                restrictedToMinimumLevel: LogEventLevel.Warning,
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} " +
                "{Properties:j}{NewLine}{Exception}")
            .WriteTo.File(
                "logs/ludo-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] " +
                "{SourceContext} {Message:lj} {Properties:j}" +
                "{NewLine}{Exception}")
            .CreateLogger();
    }
}