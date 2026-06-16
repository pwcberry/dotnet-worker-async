using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Brunsnik.SimpleWorker.Logging;

/// <summary>
/// A class that sets up log4net as a logging provider for the host services.
/// </summary>
/// <param name="config"></param>
[ProviderAlias("log4net")]
public sealed class Log4NetProvider(IOptionsMonitor<Log4NetConfiguration> config) : ILoggerProvider
{
    private readonly Log4NetConfiguration _currentConfig = config.CurrentValue;
    private readonly ConcurrentDictionary<string, Log4NetLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

    public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, name => new Log4NetLogger(name, _currentConfig));

    public Log4NetConfiguration GetCurrentConfig() => _currentConfig;

    public void Dispose()
    {
        _loggers.Clear();
    }
}
