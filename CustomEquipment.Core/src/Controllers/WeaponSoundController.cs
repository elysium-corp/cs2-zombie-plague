using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Registry;
using CustomEquipment.Services;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.ProtobufDefinitions;
using SwiftlyS2.Shared.Sounds;

namespace CustomEquipment.Controllers;

internal sealed class WeaponSoundController(
    ISwiftlyCore core,
    IEquipmentService equipmentService,
    IItemRegistry itemRegistry
) : IWeaponSoundController, IDisposable
{
    private const uint SoundEventNameHashSeed = 0x53524332;
    private const string SoundEventsResource = "soundevents/game_sounds_elysium_weapons.vsndevts";

    private readonly List<Guid> _gameEventHooks = [];
    private readonly Dictionary<(int PlayerId, string Trigger), int> _lastEmitTicks = [];
    private Guid _soundMessageHook = Guid.Empty;

    public void Initialize()
    {
        _gameEventHooks.Add(core.GameEvent.HookPost<EventWeaponFire>(OnWeaponFire));
        _gameEventHooks.Add(core.GameEvent.HookPost<EventWeaponReload>(OnWeaponReload));
        _gameEventHooks.Add(core.GameEvent.HookPost<EventWeaponFireOnEmpty>(OnWeaponFireOnEmpty));
        _gameEventHooks.Add(core.GameEvent.HookPost<EventItemEquip>(OnItemEquip));
        _gameEventHooks.Add(core.GameEvent.HookPost<EventInspectWeapon>(OnInspectWeapon));
        _gameEventHooks.Add(core.GameEvent.HookPost<EventWeaponZoom>(OnWeaponZoom));
        _gameEventHooks.Add(core.GameEvent.HookPost<EventWeaponZoomRifle>(OnWeaponZoomRifle));
        _gameEventHooks.Add(core.GameEvent.HookPost<EventSilencerOn>(OnSilencerOn));
        _gameEventHooks.Add(core.GameEvent.HookPost<EventSilencerOff>(OnSilencerOff));
        _gameEventHooks.Add(core.GameEvent.HookPost<EventSilencerDetach>(OnSilencerDetach));

        _soundMessageHook = core.NetMessage.HookServerMessage<CMsgSosStartSoundEvent>(OnStartSoundEvent);
        core.Event.OnPrecacheResource += OnPrecacheResource;
    }

    public void Dispose()
    {
        core.Event.OnPrecacheResource -= OnPrecacheResource;

        foreach (var hook in _gameEventHooks)
        {
            core.GameEvent.Unhook(hook);
        }

        _gameEventHooks.Clear();
        _lastEmitTicks.Clear();

        if (_soundMessageHook != Guid.Empty)
        {
            core.NetMessage.Unhook(_soundMessageHook);
            _soundMessageHook = Guid.Empty;
        }
    }

    private HookResult OnWeaponFire(EventWeaponFire @event) =>
        Emit(@event.UserIdPlayer, WeaponSoundTriggers.Fire);

    private HookResult OnWeaponReload(EventWeaponReload @event) =>
        Emit(@event.UserIdPlayer, WeaponSoundTriggers.Reload);

    private HookResult OnWeaponFireOnEmpty(EventWeaponFireOnEmpty @event) =>
        Emit(@event.UserIdPlayer, WeaponSoundTriggers.Empty);

    private HookResult OnItemEquip(EventItemEquip @event) =>
        Emit(@event.UserIdPlayer, WeaponSoundTriggers.Draw);

    private HookResult OnInspectWeapon(EventInspectWeapon @event) =>
        Emit(@event.UserIdPlayer, WeaponSoundTriggers.Inspect);

    private HookResult OnWeaponZoom(EventWeaponZoom @event) =>
        Emit(@event.UserIdPlayer, WeaponSoundTriggers.Zoom);

    private HookResult OnWeaponZoomRifle(EventWeaponZoomRifle @event) =>
        Emit(@event.UserIdPlayer, WeaponSoundTriggers.Zoom);

    private HookResult OnSilencerOn(EventSilencerOn @event) =>
        Emit(@event.UserIdPlayer, WeaponSoundTriggers.SilencerOn);

    private HookResult OnSilencerOff(EventSilencerOff @event) =>
        Emit(@event.UserIdPlayer, WeaponSoundTriggers.SilencerOff);

    private HookResult OnSilencerDetach(EventSilencerDetach @event) =>
        Emit(@event.UserIdPlayer, WeaponSoundTriggers.SilencerOff);

    private HookResult Emit(IPlayer? player, string trigger)
    {
        if (player is null || !player.IsValid)
        {
            return HookResult.Continue;
        }

        var weapon = equipmentService.GetActiveItem<WeaponItemBase>(player);
        var sound = weapon?.Sounds.FirstOrDefault(candidate =>
            string.Equals(candidate.Trigger, trigger, StringComparison.OrdinalIgnoreCase)
        );

        if (weapon is null || sound is null)
        {
            return HookResult.Continue;
        }

        var emitKey = (player.PlayerID, trigger);
        var tick = core.Engine.GlobalVars.TickCount;

        if (_lastEmitTicks.TryGetValue(emitKey, out var lastTick) && lastTick == tick)
        {
            return HookResult.Continue;
        }

        _lastEmitTicks[emitKey] = tick;

        using var soundEvent = new SoundEvent(sound.EventName)
        {
            SourceEntityIndex = (int)weapon.AttachedWeapon.Index
        };

        soundEvent.Recipients.AddAllPlayers();
        soundEvent.Emit();

        return HookResult.Continue;
    }

    private HookResult OnStartSoundEvent(CMsgSosStartSoundEvent message)
    {
        var weapon = ResolveWeaponBySoundSource(message.SourceEntityIndex);

        if (weapon is null)
        {
            return HookResult.Continue;
        }

        var customHashes = weapon.Sounds
            .Select(sound => MurmurHash2.HashStringLowercase(sound.EventName, SoundEventNameHashSeed));

        if (customHashes.Contains(message.SoundeventHash))
        {
            return HookResult.Continue;
        }

        var replacesHashes = weapon.Sounds
            .Select(sound => sound.ReplacesEventName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => MurmurHash2.HashStringLowercase(name!, SoundEventNameHashSeed));

        return replacesHashes.Contains(message.SoundeventHash)
            ? HookResult.Stop
            : HookResult.Continue;
    }

    private WeaponItemBase? ResolveWeaponBySoundSource(int sourceEntityIndex)
    {
        if (sourceEntityIndex < 0)
        {
            return null;
        }

        var weapon = equipmentService.GetWeaponByEntityIndex((uint)sourceEntityIndex);

        if (weapon is not null)
        {
            return weapon;
        }

        foreach (var player in core.PlayerManager.GetAllValidPlayers())
        {
            var pawnIndex = player.PlayerPawn?.Index;

            if (pawnIndex == (uint)sourceEntityIndex)
            {
                return equipmentService.GetActiveItem<WeaponItemBase>(player);
            }
        }

        return null;
    }

    private void OnPrecacheResource(IOnPrecacheResourceEvent @event)
    {
        var weapons = itemRegistry.GetDefinitions()
            .OfType<WeaponItemBase>()
            .ToArray();
        var sounds = weapons
            .SelectMany(weapon => weapon.Sounds)
            .ToArray();

        foreach (var path in weapons
                     .SelectMany(weapon => new[]
                     {
                         weapon.Model,
                         weapon.Particle?.Trace,
                         weapon.Particle?.Impact,
                         weapon.Particle?.MuzzleFlash
                     })
                     .Where(path => !string.IsNullOrWhiteSpace(path))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            @event.AddItem(path!);
        }

        if (sounds.Length == 0)
        {
            return;
        }

        @event.AddItem(SoundEventsResource);

        foreach (var path in sounds
                     .Where(sound => sound.PreloadVsnds)
                     .SelectMany(sound => sound.Files)
                     .Select(file => file.Path)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            @event.AddItem(path);
        }
    }
}
