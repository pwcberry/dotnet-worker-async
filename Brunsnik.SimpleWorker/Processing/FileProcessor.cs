using Brunsnik.SimpleWorker.Conversion;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileSystemGlobbing;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Brunsnik.SimpleWorker.Processing;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1873:Avoid potentially expensive logging", Justification = "Logger is always available.")]

public class FileProcessor(ILogger<FileProcessor> logger, ProcessorContext context) : IDisposable
{
    // FileSystemWatcher to monitor the input directory for new files. It raises events when files are created, moved, or deleted.
    private readonly FileSystemWatcher watcher = new(context.InputDirectory);

    // SafeFileAccessor to manage concurrent access to files, ensuring that only one thread can read a file at a time.
    private readonly SafeFileAccessor fileAccessor = new();

    // Flag to indicate disposal state of the FileProcessor
    private bool disposed;

    public void StartWatch()
    {
        logger.LogInformation("Watching input directory: {InputDirectory}", watcher.Path);

        watcher.Filter = $"*{context.InputFileExtension}";
        watcher.EnableRaisingEvents = true;
        watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.CreationTime | NotifyFilters.LastAccess;
        watcher.Created += OnFileCreated;
        watcher.Deleted += OnFileDeleted;
    }

    public async Task ProcessMissedFiles()
    {
        var inputDirectoryInfo = new DirectoryInfo(context.InputDirectory);
        var inputFiles = inputDirectoryInfo.GetFiles($"*{context.InputFileExtension}", SearchOption.TopDirectoryOnly);

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
        logger.LogInformation("{STATUS}: Begin conversion of: '{FileName}'", status, fileName);

        using var stream = await fileAccessor.ReadAsync(filePath, CancellationToken.None);
        var parser = new Parser();
        var document = parser.Parse(stream);
        stream.Close();

        if (document is not null)
        {
            logger.LogDebug("Converted input file: {FileName}", fileName);
            WriteDocumentToOutput(document, filePath);
        }

        if (!context.IsRetainable)
        {
            MoveInputFileToProcessedDirectory(filePath);
        }

        logger.LogInformation("{STATUS}: End conversion of: '{FileName}'", status, fileName);
    }

    private void WriteDocumentToOutput(XDocument document, string inputFilePath)
    {
        var outputFileName = Path.GetFileNameWithoutExtension(inputFilePath) + ".xml";
        var outputFilePath = Path.Combine(context.OutputDirectory, outputFileName);
        logger.LogInformation("Writing converted document to: {OutputFilename}", outputFileName);
        document.Save(outputFilePath);
    }

    private void MoveInputFileToProcessedDirectory(string inputFilePath)
    {
        var inputFileName = Path.GetFileName(inputFilePath);
        var processedFilePath = Path.Combine(context.ProcessedDirectory, inputFileName);
        logger.LogInformation("Moving input file to processed directory: {FileName}", inputFileName);
        File.Move(inputFilePath, processedFilePath, true);
    }

    private InputFileState CanConvertFile(string fileName)
    {
        if (File.Exists(Path.Join(context.ProcessedDirectory, fileName)))
        {
            logger.LogWarning("Input file was already processed: {FileName}", fileName);
            return InputFileState.Processed;
        }

        var fullPath = Path.Combine(context.InputDirectory, fileName);
        if (!File.Exists(fullPath))
        {
            logger.LogDebug("Input file not found: {FileName}", fileName);
            return InputFileState.NotFound;
        }

        if (fileAccessor.HasLock(fullPath))
        {
            logger.LogWarning("Input file is already being processed: {FileName}", fileName);
            return InputFileState.InProcess;
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
            fileAccessor.Dispose();
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