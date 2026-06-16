using log4net;
using log4net.Config;
using log4net.Core;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using System.Reflection;

namespace Brunsnik.SimpleWorker.Logging;

/// <summary>
/// Class that represents the log4net configuration for integration with the host services.
/// </summary>
public class Log4NetConfiguration
{
    private readonly ILoggerRepository _logRepository;

    public Log4NetConfiguration()
    {
#pragma warning disable CS8604 // Possible null reference argument.
        _logRepository = LogManager.GetRepository(Assembly.GetExecutingAssembly());
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning disable S4792 // Configuration is safe.
        XmlConfigurator.Configure(_logRepository,
            new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config")));
#pragma warning restore S4792 // Configuration is safe.
    }

    /// <summary>
    /// Gets a value indicating whether log4net has been configured.
    /// </summary>
    public bool IsConfigured => _logRepository.Configured;

    /// <summary>
    /// Gets a reference to the underlying log4net repository.
    /// </summary>

    public ILoggerRepository Repository => _logRepository;

    /// <summary>
    /// Checks if a specific log level is configured or enabled.
    /// </summary>
    /// <param name="logLevel">The host-specific log level.</param>
    /// <returns>Returns <c>True</c> if the log level is enabled; otherwise, <c>False</c>.</returns>
    public bool IsEnabled(LogLevel logLevel)
    {
        var level = logLevel switch
        {
            LogLevel.Trace => Level.Trace,
            LogLevel.Debug => Level.Debug,
            LogLevel.Information => Level.Info,
            LogLevel.Warning => Level.Warn,
            LogLevel.Error => Level.Error,
            LogLevel.Critical => Level.Fatal,
            _ => Level.Off,
        };

        return level != Level.Off && ((Hierarchy)_logRepository).Root.IsEnabledFor(level);
    }
}