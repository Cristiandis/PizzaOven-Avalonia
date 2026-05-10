using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PizzaOven;

public static class HttpClientExtensions
{
    public static long GetDirectorySize(this System.IO.DirectoryInfo directoryInfo, bool recursive = true)
    {
        var size = 0L;
        if (directoryInfo == null || !directoryInfo.Exists) return size;
        foreach (var f in directoryInfo.GetFiles())
            Interlocked.Add(ref size, f.Length);
        if (recursive)
            Parallel.ForEach(directoryInfo.GetDirectories(), sub =>
                Interlocked.Add(ref size, GetDirectorySize(sub, recursive)));
        return size;
    }

    public static async Task DownloadAsync(this HttpClient client, string requestUri, Stream destination,
        string fileName, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var contentLength = response.Content.Headers.ContentLength;
        using var download = await response.Content.ReadAsStreamAsync(cancellationToken);

        if (progress == null || !contentLength.HasValue)
        {
            await download.CopyToAsync(destination, cancellationToken);
            return;
        }

        var relativeProgress = new Progress<long>(totalBytes =>
            progress.Report(new DownloadProgress((float)totalBytes / contentLength.Value, totalBytes, contentLength.Value, fileName)));
        await download.CopyToAsync(destination, 81920, relativeProgress, cancellationToken);
        progress.Report(new DownloadProgress(1, contentLength.Value, contentLength.Value, fileName));
    }
}

public static class StreamExtensions
{
    public static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize,
        IProgress<long>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        if (!source.CanRead)      throw new ArgumentException("Has to be readable", nameof(source));
        if (!destination.CanWrite) throw new ArgumentException("Has to be writable", nameof(destination));
        if (bufferSize < 0)       throw new ArgumentOutOfRangeException(nameof(bufferSize));

        var buffer = new byte[bufferSize];
        long totalRead = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) != 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            totalRead += read;
            progress?.Report(totalRead);
        }
    }
}
