using Brunsnik.SimpleWorker.Processing;

namespace Brunsnik.SimpleWorker;

public class Worker(ILogger<Worker> logger, FileProcessor processor) : BackgroundService
{
    private bool isProcessorInitialized = false;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!isProcessorInitialized)
        {
            logger.LogInformation("Initializing file processor");

            await processor.ProcessMissedFiles();

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

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Message}\n{StackTrace}", ex.Message, ex.StackTrace);
            Environment.Exit(1);
        }
    }
}
