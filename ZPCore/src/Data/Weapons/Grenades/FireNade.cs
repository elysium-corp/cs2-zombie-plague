using ZPCore.Data.Effects;
using ZPCore.Data.Extensions;
using ZPCore.Data.Managers;
using ZPCore.Data.Weapons.Contracts;
using ZPCore.Data.Weapons.Enums;
using ZPCore.Data.Weapons.Utils;
using ZPCore.Di;
using ZPCore.Utils;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace ZPCore.Data.Weapons.Grenades;

internal sealed class FireNade : BaseGrenade, IWeaponPurchasable
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

        var players = MathAlgorithm.FindAllPlayersInSphere(275.0f, position);
        
        players.ForEach(player =>
        {
            if (player.IsInfected())
            {
                effectManager.ApplyEffect<Burn>(attacker, player);
            }
        });
    }
}