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
using SwiftlyS2.Shared.SchemaDefinitions;
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
    private HashSet<(ulong SessionId, WeaponItemBase Weapon)> _reloadingWeapons = [];
    private HashSet<(ulong SessionId, WeaponItemBase Weapon)> _currentReloadingWeapons = [];
    private Guid _soundMessageHook = Guid.Empty;
    private Guid _fireBulletsMessageHook = Guid.Empty;
    private bool _initialized;

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        _gameEventHooks.Add(core.GameEvent.HookPost<EventWeaponFireOnEmpty>(OnWeaponFireOnEmpty));
        _gameEventHooks.Add(core.GameEvent.HookPost<EventItemEquip>(OnItemEquip));
        _gameEventHooks.Add(core.GameEvent.HookPost<EventInspectWeapon>(OnInspectWeapon));
        _gameEventHooks.Add(core.GameEvent.HookPost<EventWeaponZoom>(OnWeaponZoom));
        _gameEventHooks.Add(core.GameEvent.HookPost<EventWeaponZoomRifle>(OnWeaponZoomRifle));
        _gameEventHooks.Add(core.GameEvent.HookPost<EventSilencerOn>(OnSilencerOn));
        _gameEventHooks.Add(core.GameEvent.HookPost<EventSilencerOff>(OnSilencerOff));
        _gameEventHooks.Add(core.GameEvent.HookPost<EventSilencerDetach>(OnSilencerDetach));

        _soundMessageHook = core.NetMessage.HookServerMessage<CMsgSosStartSoundEvent>(OnStartSoundEvent);
        _fireBulletsMessageHook = core.NetMessage.HookServerMessage<CMsgTEFireBullets>(OnFireBullets);
        core.Event.OnPrecacheResource += OnPrecacheResource;
        core.Event.OnTick += OnTick;
        core.Event.OnMapLoad += OnMapLoad;
    }

    public void Dispose()
    {
        if (!_initialized)
        {
            return;
        }

        _initialized = false;
        core.Event.OnPrecacheResource -= OnPrecacheResource;
        core.Event.OnTick -= OnTick;
        core.Event.OnMapLoad -= OnMapLoad;

        foreach (var hook in _gameEventHooks)
        {
            core.GameEvent.Unhook(hook);
        }

        _gameEventHooks.Clear();
        _lastEmitTicks.Clear();
        _reloadingWeapons.Clear();
        _currentReloadingWeapons.Clear();

        if (_soundMessageHook != Guid.Empty)
        {
            core.NetMessage.Unhook(_soundMessageHook);
            _soundMessageHook = Guid.Empty;
        }

        if (_fireBulletsMessageHook != Guid.Empty)
        {
            core.NetMessage.Unhook(_fireBulletsMessageHook);
            _fireBulletsMessageHook = Guid.Empty;
        }
    }

    private HookResult OnFireBullets(CMsgTEFireBullets message)
    {
        if ((WeaponSound_t)message.SoundType is WeaponSound_t.WEAPON_SOUND_EMPTY
            or WeaponSound_t.WEAPON_SOUND_SECONDARY_EMPTY)
        {
            return HookResult.Continue;
        }

        // Сообщение содержит упакованные дескрипторы сущностей, а не индексы игроков.
        var pawnHandle = CHandle<CCSPlayerPawn>.FromPackedInt((int)message.Player);
        var weaponHandle = CHandle<CCSWeaponBase>.FromPackedInt((int)message.WeaponId);

        if (pawnHandle.Raw == 0 || weaponHandle.Raw == 0)
        {
            return HookResult.Continue;
        }

        var player = pawnHandle.Value?.ToPlayer();

        if (player is null || !player.IsValid)
        {
            return HookResult.Continue;
        }

        var firedWeapon = weaponHandle.Value;
        var weapon = equipmentService.GetActiveItem<WeaponItemBase>(player);

        if (firedWeapon is null || weapon is null || weapon.AttachedWeapon.Address != firedWeapon.Address)
        {
            return HookResult.Continue;
        }

        // Остаток патронов может быть нулевым после настоящего выстрела последним патроном.
        return Emit(player, WeaponSoundTriggers.Fire);
    }

    private void OnTick()
    {
        _currentReloadingWeapons.Clear();

        foreach (var player in core.PlayerManager.GetAllValidPlayers())
        {
            if (!player.IsAlive)
            {
                continue;
            }

            var weapon = equipmentService.GetActiveItem<WeaponItemBase>(player);

            if (weapon is null || weapon.AttachedWeapon is not { IsValid: true, InReload: true })
            {
                continue;
            }

            var key = (player.SessionId, weapon);
            _currentReloadingWeapons.Add(key);

            // Состояние оружия учитывает и ручную, и автоматическую перезарядку.
            if (!_reloadingWeapons.Contains(key))
            {
                Emit(player, WeaponSoundTriggers.Reload);
            }
        }

        (_reloadingWeapons, _currentReloadingWeapons) = (_currentReloadingWeapons, _reloadingWeapons);
        _currentReloadingWeapons.Clear();
    }

    private void OnMapLoad(IOnMapLoadEvent @event)
    {
        _lastEmitTicks.Clear();
        _reloadingWeapons.Clear();
        _currentReloadingWeapons.Clear();
    }

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
            SourceEntityIndex = (int)weapon.AttachedWeapon.Index,
            Volume = sound.Volume
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
