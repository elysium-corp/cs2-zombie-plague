using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Services;

/// <summary>
/// Сохраняет патроны оружия выживших игроков между раундами. CS2 при перезапуске раунда
/// пополняет резерв оставшегося оружия, из-за чего покупка патронов теряла смысл.
/// </summary>
internal sealed class RoundAmmoPreserver(ISwiftlyCore core) : IDisposable
{
    private readonly RoundAmmoLedger _ledger = new();
    private Guid _prestartHook = Guid.Empty;
    private Guid _poststartHook = Guid.Empty;
    private int _generation;
    private bool _initialized;

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        _generation++;
        _prestartHook = core.GameEvent.HookPre<EventRoundPrestart>(OnRoundPrestart);
        _poststartHook = core.GameEvent.HookPost<EventRoundPoststart>(OnRoundPoststart);
        core.Event.OnMapLoad += OnMapLoad;
    }

    public void Dispose()
    {
        if (!_initialized)
        {
            return;
        }

        _initialized = false;
        _generation++;
        core.GameEvent.Unhook(_prestartHook);
        core.GameEvent.Unhook(_poststartHook);
        core.Event.OnMapLoad -= OnMapLoad;
        _ledger.Clear();
    }

    // round_prestart приходит до возрождения игроков и пополнения патронов.
    private HookResult OnRoundPrestart(EventRoundPrestart @event)
    {
        _ledger.Clear();

        foreach (var (player, weapon) in CarriedFirearms())
        {
            _ledger.Record(player.SessionId, weapon.Index, weapon.DesignerName, Read(weapon));
        }

        return HookResult.Continue;
    }

    // round_poststart завершает перезапуск раунда: к этому моменту оружие выживших уже пополнено.
    private HookResult OnRoundPoststart(EventRoundPoststart @event)
    {
        if (_ledger.Count == 0)
        {
            return HookResult.Continue;
        }

        Restore();

        // Повтор на следующем кадре закрывает пополнение, выполненное после события;
        // восстановление не добавляет патронов, поэтому повтор безопасен.
        var generation = _generation;
        core.Scheduler.NextWorldUpdate(() =>
        {
            if (!_initialized || generation != _generation)
            {
                return;
            }

            Restore();
            _ledger.Clear();
        });

        return HookResult.Continue;
    }

    private void OnMapLoad(IOnMapLoadEvent @event) => _ledger.Clear();

    private void Restore()
    {
        foreach (var (player, weapon) in CarriedFirearms())
        {
            if (!_ledger.TryRestore(player.SessionId, weapon.Index, weapon.DesignerName, Read(weapon), out var ammo))
            {
                continue;
            }

            weapon.Clip1 = ammo.Clip1;
            weapon.Clip2 = ammo.Clip2;
            weapon.ReserveAmmo[0] = ammo.Reserve1;
            weapon.ReserveAmmo[1] = ammo.Reserve2;
            weapon.Clip1Updated();
            weapon.Clip2Updated();
            weapon.ReserveAmmoUpdated();
        }
    }

    private IEnumerable<(IPlayer Player, CCSWeaponBase Weapon)> CarriedFirearms()
    {
        foreach (var player in core.PlayerManager.GetAllPlayers())
        {
            if (player is not { IsValid: true, IsAlive: true } ||
                player.PlayerPawn is not { IsValid: true, WeaponServices: { } weapons })
            {
                continue;
            }

            foreach (var carried in weapons.MyValidWeapons)
            {
                // Нож, гранаты и снаряжение не имеют магазина и не пополняются движком.
                if (carried.As<CCSWeaponBase>() is { IsValidEntity: true } weapon &&
                    weapon.WeaponBaseVData.MaxClip1 > 0)
                {
                    yield return (player, weapon);
                }
            }
        }
    }

    private static WeaponAmmo Read(CCSWeaponBase weapon)
    {
        return new WeaponAmmo(weapon.Clip1, weapon.Clip2, weapon.ReserveAmmo[0], weapon.ReserveAmmo[1]);
    }
}
