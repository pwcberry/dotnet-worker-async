using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Brunsnik.SimpleWorker.Processing;

/// <summary>
/// 
/// </summary>
/// <remarks>
/// <para>Here are the key design decisions worth calling out:</para>
/// <para>Path normalization — Path.GetFullPath ensures that "./log.txt", "log.txt", and "/app/log.txt" (if they resolve to the same file) all map to the same dictionary key, avoiding phantom duplicates.</para>
/// <para>GetOrAdd is safe here — ConcurrentDictionary.GetOrAdd is atomic for the key insertion, but the factory delegate can run more than once under high contention. That's fine in this case since a SemaphoreSlim is cheap and the dictionary will only ever keep one winner per key.</para>
/// <para>Dispose is important — Each SemaphoreSlim holds a kernel object. Without Dispose, you leak those handles for every path that was ever accessed, so implementing IDisposable (or IAsyncDisposable) and clearing the dictionary is essential for long-running processes.</para>
/// <para>Independent files don't block each other — task1 writing to log.txt and task2 writing to data.txt hold different semaphores, so they proceed in parallel. Only threads targeting the same path are serialized.</para>
/// </remarks>
public sealed class SafeFileAccessor : IDisposable
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> fileLocks = new();
    private bool disposed;

    public async Task<TextReader> ReadAsync(string filePath, CancellationToken cancellationToken)
    {
        var fileLock = GetLock(filePath);

        await fileLock.WaitAsync(cancellationToken);

        var stream = await OpenFileStreamAsync(filePath, cancellationToken);
        try
        {
            return new StreamReader(stream, Encoding.UTF8, bufferSize: 4096, leaveOpen: false);
        }
        catch
        {
            await stream.DisposeAsync();
            throw;
        }
        finally
        {
            fileLock.Release();
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            foreach (var semaphore in fileLocks.Values)
            {
                semaphore.Dispose();
            }

            fileLocks.Clear();
            disposed = true;
        }
    }

    private SemaphoreSlim GetLock(string filePath)
    {
        // Normalize the path to prevent duplicates from e.g. different casing or separators
        string key = Path.GetFullPath(filePath);
        return fileLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
    }

    private static Task<FileStream> OpenFileStreamAsync(string filePath, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        }, cancellationToken);
    }
}
