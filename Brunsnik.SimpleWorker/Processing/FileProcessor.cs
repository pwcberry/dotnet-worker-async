using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileSystemGlobbing;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;

namespace Brunsnik.SimpleWorker.Processing;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1873:Avoid potentially expensive logging", Justification = "Logger is always available.")]

public class FileProcessor(ILogger<FileProcessor> logger, ProcessorContext context) : IDisposable
{
    // FileSystemWatcher to monitor the input directory for new files. It raises events when files are created, moved, or deleted.
    private readonly FileSystemWatcher watcher = new(context.InputDirectory);

    // Used as a lock to ensure that only one file is processed at a time, and to prevent multiple threads from processing the same file simultaneously.
    private readonly SemaphoreSlim semaphore = new(1, 1);

    // Used to track files that are currently being processed, to prevent multiple threads from processing the same file simultaneously.
    private readonly HashSet<string> filesInProcess = new();

    // Used to synchronize access to the filesInProcess HashSet, since it is not thread-safe.
    private readonly Lock hashSetLock = new();

    // Flag to indicate disposal state of the FileProcessor
    private bool disposed;

    public void StartWatch()
    {
        logger.LogInformation("Watching input directory: {InputDirectory}", watcher.Path);

        watcher.Filter = $"*.dat";
        watcher.EnableRaisingEvents = true;
        watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.CreationTime | NotifyFilters.LastAccess;
        watcher.Created += OnFileCreated;
        watcher.Deleted += OnFileDeleted;
    }

    public async Task ProcessMissedFiles()
    {
        var inputDirectoryInfo = new DirectoryInfo(context.InputDirectory);
        var inputFiles = inputDirectoryInfo.GetFiles($"*.dat", SearchOption.TopDirectoryOnly);

        logger.LogInformation("Reviewing files in the input directory: {InputDirectory}", context.InputDirectory);

        foreach (var fileInfo in inputFiles)
        {
            logger.LogInformation("Reviewing: {FileName}", fileInfo.Name);

            if (CanConvertFile(fileInfo.Name) == InputFileState.Ready)
            {
                await ConvertFile(fileInfo.FullName, "REVIEWING");
            }
        }
    }

    private async Task ConvertFile(string filePath, string status)
    {
        var fileName = Path.GetFileName(filePath);
        logger.LogInformation("{STATUS}: Begin conversion of: '{Path}'", status, filePath);

        lock (hashSetLock)
        {
            if (!filesInProcess.Contains(fileName))
            {
                filesInProcess.Add(fileName);
            }
        }

        await semaphore.WaitAsync();
        logger.LogInformation("{STATUS}: Semaphore acquired", status);

        if (File.Exists(filePath))
        {
            logger.LogInformation("{STATUS}: File exists: {FileName}", status, fileName);

            try
            {
                await ConvertInputFile(filePath, status);

                var processedFilePath = Path.Combine(context.ProcessedDirectory, fileName);
                File.Move(filePath, processedFilePath, overwrite: true);
                logger.LogInformation("{STATUS}: Moved original file to processed directory: {FileName}", status, fileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{STATUS}: Error processing file: {FileName}\nError: {Message}", status, Path.GetFileName(filePath), ex.Message);
            }
            finally
            {
                semaphore.Release();
                logger.LogInformation("{STATUS}: Semaphore released", status);
            }
        }
    }

    private async Task ConvertInputFile(string inputFilePath, string status)
    {
        var fileName = Path.GetFileName(inputFilePath);
        logger.LogInformation("{STATUS}: Begin converting file   : {FileName}", status, fileName);

        await Task.Delay(TimeSpan.FromSeconds(5));

        var outputFilePath = Path.Combine(context.OutputDirectory, fileName);
        File.Copy(inputFilePath, outputFilePath, overwrite: true);
        logger.LogInformation("{STATUS}: Finished converting file: {FileName}", status, fileName);
    }

    private InputFileState CanConvertFile(string fileName)
    {
        if (!File.Exists(Path.Combine(context.InputDirectory, fileName)))
        {
            logger.LogDebug("Input file not found: {FileName}", fileName);
            return InputFileState.NotFound;
        }

        var processedFile = new FileInfo(Path.Join(context.ProcessedDirectory, fileName));
        if (processedFile.Exists)
        {
            logger.LogWarning("Input file was already processed: {FileName}", fileName);
            return InputFileState.Processed;
        }

        lock (hashSetLock)
        {
            if (filesInProcess.Contains(fileName))
            {
                logger.LogWarning("Input file is already being processed: {FileName}", fileName);
                return InputFileState.InProcess;
            }
        }

        return InputFileState.Ready;
    }

    #region FileSystemWatcher event handlers
    private async void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        logger.LogInformation("Input file found: {FileName}", e.Name);

        if (CanConvertFile(e.Name!) == InputFileState.Ready)
        {
            await ConvertFile(e.FullPath, "CREATED");
        }
    }

    private async void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        logger.LogInformation("Input file moved or deleted: {FileName}", e.Name);
    }
    #endregion

    #region IDisposable
    public void Dispose()
    {
        if (!disposed && watcher is not null)
        {
            watcher.Created -= OnFileCreated;
            watcher.Deleted -= OnFileDeleted;
            watcher.Dispose();
            semaphore.Dispose();
            disposed = true;
        }

        GC.SuppressFinalize(this);
    }
    #endregion
}

internal enum InputFileState
{
    NotFound,
    Ready,
    InProcess,
    Processed
}