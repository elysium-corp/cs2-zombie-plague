namespace Menu.Core.Storage;

internal static class AtomicFileWriter
{
    public static async ValueTask WriteAsync(
        string path,
        ReadOnlyMemory<byte> contents,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var destinationPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(destinationPath)
            ?? throw new ArgumentException("The destination must have a parent directory.", nameof(path));

        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(destinationPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(
                             temporaryPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 64 * 1024,
                             options: FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(contents, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, destinationPath, overwrite: true);
        }
        finally
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (IOException)
            {
                // Остаточный temporary-файл не влияет на уже активированный snapshot.
            }
            catch (UnauthorizedAccessException)
            {
                // Ошибка очистки не должна скрывать результат основной операции записи.
            }
        }
    }
}
