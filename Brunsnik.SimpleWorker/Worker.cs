using Brunsnik.SimpleWorker.Processing;

namespace Brunsnik.SimpleWorker;

public class Worker(ILogger<Worker> logger, FileProcessor processor, IConfiguration config) : BackgroundService
{
    private bool isProcessorInitialized = false;
    private int workerIntervalInSeconds;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!isProcessorInitialized)
        {
            logger.LogInformation("Initializing file processor");

            // Read the worker interval from the configuration; the default is 5 minutes (300 seconds).
            workerIntervalInSeconds = config.GetValue<int>("Settings:WorkerIntervalInSeconds", 300);

            // Process any files that are in the input directory that were not processed before the service started.
            await processor.ProcessMissedFiles();

            // Start watching for new files in the input directory.
            processor.StartWatch();

            isProcessorInitialized = true;
        }

        await base.StartAsync(cancellationToken);
    }
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (isProcessorInitialized)
        {
            logger.LogInformation("Deactivating file processor");
            processor.Dispose();
        }

        await base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogDebug("Checking for unprocessed files");
                await processor.ProcessMissedFiles();

                await Task.Delay(TimeSpan.FromSeconds(workerIntervalInSeconds), stoppingToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Message}\n{StackTrace}", ex.Message, ex.StackTrace);
            Environment.Exit(1);
        }
    }
}
