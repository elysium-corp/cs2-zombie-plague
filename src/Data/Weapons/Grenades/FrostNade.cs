using CS2ZombiePlague.Data.Effects;
using CS2ZombiePlague.Data.Extensions;
using CS2ZombiePlague.Data.Managers;
using CS2ZombiePlague.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Sounds;
using Vector = SwiftlyS2.Shared.Natives.Vector;

namespace CS2ZombiePlague.Data.Weapons.Grenades;

public class FrostNade(ISwiftlyCore core, CommonUtils commonUtils) : ICustomWeapon, IGrenade
{
    public string OriginalName => "hegrenade_projectile";
    public string IternalName => "weapon_frost_nade";
    public string DisplayName => "FrostNade";
    
    private readonly EffectManager _effectManager = DependencyManager.GetService<EffectManager>();

    private const float ExplodeRadius = 250.0f;

    public void Load()
    {
        core.GameEvent.HookPre<EventHegrenadeDetonate>(PreEventGrenadeDetonate);
        core.Event.OnEntityTakeDamage += TakeDamage;
    }

    public void Explode(int userid, Vector position, int grenadeIndex)
    {
        var playersInRadius = commonUtils.FindAllPlayersInSphere(ExplodeRadius, position);
        PlaySound("FrostNade.detonate", grenadeIndex);

        foreach (var player in playersInRadius)
        {
            if (player.IsInfected() && !player.IsNemesis() && !player.IsFrozen())
            {
                _effectManager.ApplyEffect<Freeze>(null, player);
            }
        }
    }

    private void TakeDamage(IOnEntityTakeDamageEvent @event)
    {
        if (@event.Info.Inflictor.Value != null && @event.Info.Inflictor.Value.DesignerName == OriginalName)
        {
            @event.Info.Damage = 0;
        }
    }
    
    private HookResult PreEventGrenadeDetonate(EventHegrenadeDetonate @event)
    {
        var grenade = core.EntitySystem.GetEntityByIndex<CEntityInstance>((uint)@event.EntityID);
        if (grenade != null && grenade.IsValid)
        {
            Explode(@event.UserId, new Vector(@event.X, @event.Y, @event.Z), (int)grenade.Index);
            grenade.Despawn();
        }

        return HookResult.Handled;
    }
    
    private void PlaySound(string soundName, int entityIndex)
    {
        using var soundEvent = new SoundEvent()
        {
            Volume = 3.5f,
            Name = soundName,
            SourceEntityIndex = entityIndex
        };
        soundEvent.Recipients.AddAllPlayers();
        soundEvent.Emit();
    }
}