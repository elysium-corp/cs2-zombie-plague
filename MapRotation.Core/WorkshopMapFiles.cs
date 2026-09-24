using System.Globalization;
using System.Security;
using SwiftlyS2.Shared.SteamAPI;

namespace MapRotation.Core;

/// <summary>Результат проверки установленного содержимого Workshop для одной карты.</summary>
internal sealed record WorkshopInstallation(uint? ItemState, string? InstallDirectory, string? VpkPath, string? ErrorType)
{
    /// <summary>Содержимое установлено, не обновляется и содержит непустой корневой VPK.</summary>
    internal bool IsReady => VpkPath is not null;
}

/// <summary>Находит установленные карты по каталогу Steam без загрузки ресурсов и изменения сервера.</summary>
internal sealed class WorkshopMapFiles(Func<ulong, uint> getState, Func<ulong, string?> getInstallDirectory)
{
    private const uint Installed = 4;
    private const uint Unavailable = 2 | 16 | 32;

    /// <summary>Проверка через API Steam игрового сервера; вызывается в игровом потоке.</summary>
    internal static WorkshopMapFiles Steam { get; } = new(
        id => SteamGameServerUGC.GetItemState(new PublishedFileId_t(id)),
        id => SteamGameServerUGC.GetItemInstallInfo(new PublishedFileId_t(id), out _, out var folder, 4096, out _)
            ? folder : null);

    /// <summary>Проверяет одну установку; ошибки Steam и файловой системы не затрагивают остальные карты.</summary>
    internal WorkshopInstallation Read(long workshopId)
    {
        uint? state = null;
        string? directory = null;
        try
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(workshopId);
            state = getState((ulong)workshopId);
            // Старую установленную версию можно использовать до начала скачивания обновления.
            if ((state.Value & Installed) == 0 || (state.Value & Unavailable) != 0)
                return new(state, null, null, null);

            directory = getInstallDirectory((ulong)workshopId);
            if (string.IsNullOrWhiteSpace(directory)) return new(state, directory, null, null);
            if (!Path.IsPathFullyQualified(directory))
                return new(state, directory, null, nameof(ArgumentException));

            var id = workshopId.ToString(CultureInfo.InvariantCulture);
            foreach (var filename in new[] { id + ".vpk", id + "_dir.vpk" })
            {
                var path = Path.Combine(directory, filename);
                if (IsNonEmptyFile(path)) return new(state, directory, path, null);
            }

            // Steam может сохранить архив с именем карты; части *_000.vpk не являются индексом архива.
            foreach (var path in Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
            {
                if (!string.Equals(Path.GetExtension(path), ".vpk", StringComparison.OrdinalIgnoreCase)) continue;
                var filename = Path.GetFileNameWithoutExtension(path).AsSpan();
                if (filename.Length >= 4 && filename[^4] == '_' && char.IsAsciiDigit(filename[^3])
                    && char.IsAsciiDigit(filename[^2]) && char.IsAsciiDigit(filename[^1])) continue;
                if (IsNonEmptyFile(path)) return new(state, directory, path, null);
            }

            return new(state, directory, null, null);
        }
        catch (Exception exception) when (exception is InvalidOperationException or DllNotFoundException
            or EntryPointNotFoundException or BadImageFormatException or TypeInitializationException
            or IOException or UnauthorizedAccessException or SecurityException or ArgumentException or NotSupportedException)
        {
            return new(state, directory, null, exception.GetType().Name);
        }
    }

    private static bool IsNonEmptyFile(string path)
    {
        var file = new FileInfo(path);
        return file.Exists && file.Length > 0;
    }
}
