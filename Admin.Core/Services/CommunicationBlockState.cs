using Admin.Core.Data;

namespace Admin.Core.Services;

internal sealed class CommunicationBlockState
{
    private readonly Dictionary<(ulong SteamId, CommunicationKind Kind), DateTime?> _blocks = [];

    public void Set(ulong steamId, CommunicationKind kind, DateTime? expiresAt) => _blocks[(steamId, kind)] = expiresAt;
    public void Remove(ulong steamId, CommunicationKind kind) => _blocks.Remove((steamId, kind));
    public void Clear() => _blocks.Clear();

    public bool IsBlocked(ulong steamId, CommunicationKind kind, DateTime now)
    {
        if (!_blocks.TryGetValue((steamId, kind), out var expiresAt)) return false;
        if (expiresAt is null || expiresAt > now) return true;
        _blocks.Remove((steamId, kind));
        return false;
    }
}
