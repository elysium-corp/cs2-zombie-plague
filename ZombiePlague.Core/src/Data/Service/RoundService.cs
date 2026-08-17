using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using ZombiePlague.Core.Config.Core;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Service.Contracts;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Service;

internal interface IRoundService : IService;

internal sealed class RoundService(
    ISwiftlyCore core,
    IRoundManager roundManager,
    IOptions<ZombiePlagueCoreConfig> config) : IRoundService
{
    private bool _isRoundEnded;

    private Guid _roundStartHook = Guid.Empty;
    private Guid _roundEndHook = Guid.Empty;
    private Guid _gameRestartHook = Guid.Empty;
    private Guid _playerConnectHook = Guid.Empty;
    private Guid _playerDeathHook = Guid.Empty;
    private Guid _playerDisconnectHook = Guid.Empty;

    public void Register()
    {
        _roundStartHook = core.GameEvent.HookPost<EventRoundStart>(OnRoundStart);
        _roundEndHook = core.GameEvent.HookPost<EventRoundEnd>(OnRoundEnd);
        _gameRestartHook = core.GameEvent.HookPost<EventCsPreRestart>(OnGameRestart);
        _playerConnectHook = core.GameEvent.HookPost<EventPlayerConnectFull>(OnPlayerConnected);
        _playerDeathHook = core.GameEvent.HookPost<EventPlayerDeath>(OnPlayerDeath);
        _playerDisconnectHook = core.GameEvent.HookPost<EventPlayerDisconnect>(OnPlayerDisconnect);

        core.GameHooks.Entities.TakeDamage.Pre += OnTakeDamage;
    }

    public void Unregister()
    {
        core.GameEvent.Unhook(_roundStartHook);
        core.GameEvent.Unhook(_roundEndHook);
        core.GameEvent.Unhook(_gameRestartHook);
        core.GameEvent.Unhook(_playerConnectHook);
        core.GameEvent.Unhook(_playerDeathHook);
        core.GameEvent.Unhook(_playerDisconnectHook);

        core.GameHooks.Entities.TakeDamage.Pre -= OnTakeDamage;
    }

    private HookResult OnRoundStart(EventRoundStart @event)
    {
        _isRoundEnded = false;

        core.Scheduler.NextWorldUpdate(RemoveAllWeapons);

        roundManager.Prepare();

        PlayAmbientAll();

        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event)
    {
        _isRoundEnded = true;

        roundManager.End();

        return HookResult.Continue;
    }

    private HookResult OnGameRestart(EventCsPreRestart @event)
    {
        roundManager.End();

        return HookResult.Continue;
    }

    private HookResult OnPlayerConnected(EventPlayerConnectFull @event)
    {
        PlayAmbientLocal(@event.UserIdPlayer);

        return roundManager.OnPlayerConnected(@event);
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event)
    {
        return roundManager.OnPlayerDeath(@event);
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        return roundManager.OnPlayerDisconnect(@event);
    }

    private void OnTakeDamage(ref TakeDamageEntityPreContext context)
    {
        if (_isRoundEnded)
        {
            var victim = context.Params.Entity.Address.FindPlayerByPawnAddress();
            if (victim is not { IsValid: true }) return;

            var attacker = context.Params.Info.Attacker.ResolvePlayerFromHandle();
            if (attacker is not { IsValid: true }) return;

            context.Params.Info.Damage = 0;
            context.SetHookResult(HookResult.CancelOriginal);
        }

        roundManager.OnTakeDamage(ref context);
    }

    private void RemoveAllWeapons()
    {
        var weapons = core.EntitySystem
            .GetAllEntitiesByClass<CBasePlayerWeapon>()
            .Where(x => x.IsValidEntity && !x.OwnerEntity.IsValid)
            .ToList();

        foreach (var weapon in weapons)
        {
            weapon.Despawn();
        }
    }

    private void PlayAmbientAll()
    {
        var sound = config.Value.AmbienceSounds.GetRandomString();

        if (sound.IsNullOrEmpty()) return;

        SoundExt.PlayGlobal(sound, 1f);
    }

    private void PlayAmbientLocal(IPlayer? recipient)
    {
        if (recipient == null) return;

        var sound = config.Value.AmbienceSounds.GetRandomString();

        if (sound.IsNullOrEmpty()) return;

        SoundExt.PlayLocal(recipient, sound, 1f);
    }
}