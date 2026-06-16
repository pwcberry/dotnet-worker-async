using log4net;

namespace Brunsnik.SimpleWorker.Logging;

/// <summary>
/// A logger implementation that uses log4net as the underlying logging framework.
/// </summary>
/// <param name="name">The name of the logger context.</param>
/// <param name="config">The log4net configuration as provided by the host.</param>
public sealed class Log4NetLogger(string name, Log4NetConfiguration config) : ILogger
{
    private readonly ILog _log = LogManager.GetLogger(config.Repository.Name, name);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <summary>
    /// Checks if a specific log level is enabled.
    /// </summary>
    /// <param name="logLevel">The host-specific log level.</param>
    /// <returns>Returns <c>True</c> if the log level is enabled; otherwise, <c>False</c>.</returns>
    public bool IsEnabled(LogLevel logLevel) => config.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(formatter);

        var message = formatter(state, exception);

        if (!string.IsNullOrEmpty(message))
        {
            switch (logLevel)
            {
                case LogLevel.Debug:
                case LogLevel.Trace:
                    _log.Debug(message);
                    break;
                case LogLevel.Information:
                    _log.Info(message);
                    break;
                case LogLevel.Warning:
                    _log.Warn(message);
                    break;
                case LogLevel.Error:
                    _log.Error(message);
                    break;
                case LogLevel.Critical:
                    _log.Fatal(message);
                    break;
                default:
                    _log.Warn("Encountered unknown log level, writing out as Info.");
                    _log.Info(message, exception);
                    break;
            }
        }
    }
}