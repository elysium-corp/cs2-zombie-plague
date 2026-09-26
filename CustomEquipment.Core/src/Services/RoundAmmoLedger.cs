namespace CustomEquipment.Services;

/// <summary>Патроны оружия: магазины и резерв.</summary>
internal readonly record struct WeaponAmmo(int Clip1, int Clip2, int Reserve1, int Reserve2);

/// <summary>
/// Снимок патронов оружия, которое выжившие игроки переносят в следующий раунд.
/// Восстановление только отменяет пополнение: патронов никогда не становится больше,
/// чем было в конце раунда или стало после него.
/// </summary>
internal sealed class RoundAmmoLedger
{
    private readonly Dictionary<(ulong Session, uint Weapon), (string DesignerName, WeaponAmmo Ammo)> _saved = [];

    public int Count => _saved.Count;

    public void Record(ulong session, uint weapon, string designerName, WeaponAmmo ammo)
    {
        _saved[(session, weapon)] = (designerName, ammo);
    }

    public bool TryRestore(ulong session, uint weapon, string designerName, WeaponAmmo current, out WeaponAmmo restored)
    {
        restored = current;

        // Индекс сущности мог достаться другому оружию: восстанавливаем только тот же предмет того же игрока.
        if (!_saved.TryGetValue((session, weapon), out var saved) ||
            !string.Equals(saved.DesignerName, designerName, StringComparison.Ordinal))
        {
            return false;
        }

        restored = new WeaponAmmo(
            Math.Min(current.Clip1, saved.Ammo.Clip1),
            Math.Min(current.Clip2, saved.Ammo.Clip2),
            Math.Min(current.Reserve1, saved.Ammo.Reserve1),
            Math.Min(current.Reserve2, saved.Ammo.Reserve2)
        );

        return restored != current;
    }

    public void Clear() => _saved.Clear();
}
