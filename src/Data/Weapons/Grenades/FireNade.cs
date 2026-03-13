using CS2ZombiePlague.Data.Effects;
using CS2ZombiePlague.Data.Extensions;
using CS2ZombiePlague.Data.Managers;
using CS2ZombiePlague.Data.Weapons.Contracts;
using CS2ZombiePlague.Data.Weapons.Enums;
using CS2ZombiePlague.Data.Weapons.Utils;
using CS2ZombiePlague.Di;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Data.Weapons.Grenades;

public sealed class FireNade : BaseGrenade, IWeaponPurchasable
{
    public override string InheritorName => WeaponName.Grenade.Inc;
    
    public override string DisplayName => "Fire Nade";

    public override string InternalName => "fire_nade";

    public override WeaponSlot Slot => WeaponSlot.Grenades;
    
    public override string Model => "";

    public override WeaponRarity WeaponRarity => WeaponRarity.Modified;

    public int Coast => 1;

    public WeaponType WeaponType => WeaponType.Equipment;

    public override void OnMolotovDetonate(IPlayer attacker, Vector position)
    {
        var effectManager = DependencyManager.GetService<EffectManager>();
        var commonUtils = DependencyManager.GetService<CommonUtils>();

        var players = commonUtils.FindAllPlayersInSphere(275.0f, position);
        
        players.ForEach(player =>
        {
            if (player.IsInfected())
            {
                effectManager.ApplyEffect<Burn>(attacker, player);
            }
        });
    }
}