using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;
using Brunsnik.SimpleWorker.Logging;

namespace Brunsnik.SimpleWorker;

public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddLog4NetLogger(this ILoggingBuilder builder)
    {
        builder.AddConfiguration();
        
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, Log4NetProvider>());

        LoggerProviderOptions.RegisterProviderOptions<Log4NetConfiguration, Log4NetProvider>(builder.Services);

        return builder;
    }
}