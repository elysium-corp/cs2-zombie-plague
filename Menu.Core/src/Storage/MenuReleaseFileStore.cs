using System.Text.Json;
using Menu.Api.Contracts;
using Menu.Api.Enums;
using Menu.Api.Results;
using Menu.Core.Runtime;
using Menu.Core.Validation;

namespace Menu.Core.Storage;

internal sealed record MenuFileLoadResult(
    bool IsValid,
    MenuSnapshotSource Source,
    MenuReleaseDefinition? Release,
    MenuReleaseValidationResult Validation,
    MenuReleaseValidationContext? Context);

internal sealed class MenuReleaseFileStore
{
    private const int MaximumPayloadBytes = 16 * 1024 * 1024;
    private readonly MenuReleaseValidator _validator;

    public MenuReleaseFileStore(MenuReleaseValidator validator)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async ValueTask<MenuFileLoadResult> LoadAsync(
        string path,
        MenuSnapshotSource source,
        MenuReleaseValidationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return await LoadAsync(path, source, _ => context, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<MenuFileLoadResult> LoadAsync(
        string path,
        MenuSnapshotSource source,
        Func<MenuReleaseDefinition, MenuReleaseValidationContext> contextFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(contextFactory);
        if (source is not (MenuSnapshotSource.LastKnownGood or MenuSnapshotSource.Fallback))
        {
            throw new ArgumentOutOfRangeException(nameof(source), source, "Only local snapshot sources are supported.");
        }

        try
        {
            var file = new FileInfo(Path.GetFullPath(path));
            if (!file.Exists)
            {
                return Invalid(source, "file.not_found", "Local menu snapshot file was not found.");
            }

            if (file.Length is <= 0 or > MaximumPayloadBytes)
            {
                return Invalid(source, "file.size_invalid", "Local menu snapshot file has an invalid size.");
            }

            var bytes = await File.ReadAllBytesAsync(file.FullName, cancellationToken).ConfigureAwait(false);
            if (bytes.Length is <= 0 or > MaximumPayloadBytes)
            {
                return Invalid(source, "file.size_invalid", "Local menu snapshot file changed to an invalid size while reading.");
            }

            var release = MenuJson.DeserializeRelease(bytes);
            if (release is null)
            {
                return Invalid(source, "file.release_required", "Local menu snapshot must contain a release object.");
            }

            var context = contextFactory(release);
            ArgumentNullException.ThrowIfNull(context);
            var validation = _validator.Validate(release, context);
            return new MenuFileLoadResult(validation.IsValid, source, release, validation, context);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (JsonException exception)
        {
            return Invalid(source, "file.json_invalid", $"Local menu snapshot JSON is invalid ({exception.GetType().Name}).");
        }
        catch (IOException exception)
        {
            return Invalid(source, "file.read_failed", $"Local menu snapshot could not be read ({exception.GetType().Name}).");
        }
        catch (UnauthorizedAccessException exception)
        {
            return Invalid(source, "file.access_denied", $"Local menu snapshot could not be accessed ({exception.GetType().Name}).");
        }
    }

    public async ValueTask<MenuReleaseValidationResult> SaveValidatedAsync(
        string path,
        MenuReleaseDefinition release,
        MenuReleaseValidationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(release);
        ArgumentNullException.ThrowIfNull(context);

        var checksummed = release with
        {
            Checksum = MenuJson.ComputeChecksum(release)
        };
        var validation = _validator.Validate(checksummed, context);
        if (!validation.IsValid)
        {
            return validation;
        }

        var bytes = JsonSerializer.SerializeToUtf8Bytes(checksummed, MenuJson.SerializerOptions);
        await AtomicFileWriter.WriteAsync(path, bytes, cancellationToken).ConfigureAwait(false);
        return validation;
    }

    private static MenuFileLoadResult Invalid(
        MenuSnapshotSource source,
        string code,
        string message)
    {
        var validation = new MenuReleaseValidationResult(
        [
            new MenuValidationIssue
            {
                Severity = MenuValidationSeverity.Error,
                Code = code,
                Message = message,
                Path = "$"
            }
        ]);
        return new MenuFileLoadResult(false, source, null, validation, null);
    }

}
