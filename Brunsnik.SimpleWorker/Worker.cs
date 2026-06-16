namespace Brunsnik.SimpleWorker;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Cancellation requested");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Message}\n{StackTrace}", ex.Message, ex.StackTrace);
            Environment.Exit(1);
        }
    }
}
