using SwiftlyS2.Shared.Convars;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Services;

internal sealed class WeaponHandlingPrediction(IConVarService conVars)
{
    private const string RecoilScale = "weapon_recoil_scale";
    private const string NoSpread = "weapon_accuracy_nospread";
    private readonly Dictionary<int, (ulong SessionId, bool NoRecoil, bool NoSpread)> _overrides = [];

    internal void Update(IPlayer player, bool noRecoil, bool noSpread)
    {
        if (!player.IsValid || player.IsFakeClient)
        {
            return;
        }

        _overrides.TryGetValue(player.PlayerID, out var previous);

        if (previous.SessionId != player.SessionId)
        {
            previous = default;
        }

        var recoilApplied = UpdateOverride(player.PlayerID, RecoilScale, "0", previous.NoRecoil, noRecoil);
        var spreadApplied = UpdateOverride(player.PlayerID, NoSpread, "1", previous.NoSpread, noSpread);

        if (recoilApplied || spreadApplied)
        {
            _overrides[player.PlayerID] = (player.SessionId, recoilApplied, spreadApplied);
        }
        else
        {
            _overrides.Remove(player.PlayerID);
        }
    }

    internal void Remove(int playerId) => _overrides.Remove(playerId);

    internal void Restore(IEnumerable<IPlayer> players)
    {
        foreach (var player in players)
        {
            Update(player, false, false);
        }

        _overrides.Clear();
    }

    private bool UpdateOverride(int playerId, string name, string value, bool previous, bool enabled)
    {
        if (previous == enabled)
        {
            return previous;
        }

        var conVar = conVars.FindAsString(name);

        if (conVar is null)
        {
            return false;
        }

        // Серверные cvar не меняются. При выходе из режима возвращается текущее
        // серверное значение, а не захардкоженное значение по умолчанию.
        conVar.ReplicateToClientAsString(playerId, enabled ? value : conVar.ValueAsString);
        return enabled;
    }
}
