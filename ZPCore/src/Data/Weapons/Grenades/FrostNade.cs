using ZPCore.Data.Effects;
using ZPCore.Data.Extensions;
using ZPCore.Data.Managers;
using ZPCore.Data.Weapons.Contracts;
using ZPCore.Data.Weapons.Enums;
using ZPCore.Data.Weapons.Utils;
using ZPCore.Di;
using ZPCore.Utils;
using SwiftlyS2.Shared.Natives;

namespace ZPCore.Data.Weapons.Grenades;

internal sealed class FrostNade : BaseGrenade, IWeaponPurchasable
{
    public override string InheritorName => WeaponName.Grenade.He;
    
    public override string DisplayName => "Frost Nade";

    public override string InternalName => "frost_nade";

    public override WeaponSlot Slot => WeaponSlot.Grenades;

    public override float DamageMultiplier => 0.0f;

    public override string Model => "";

    public override WeaponRarity WeaponRarity => WeaponRarity.Modified;

    public int Coast => 1;

    public WeaponType WeaponType => WeaponType.Equipment;

    public override void OnHegrenadeDetonate(Vector position)
    {
        var effectManager = DependencyManager.GetService<EffectManager>();
        var playersInRadius = MathAlgorithm.FindAllPlayersInSphere(250.0f, position);

        foreach (var player in playersInRadius)
        {
            if (player.IsInfected() && !player.IsNemesis() && !player.IsFrozen())
            {
                effectManager.ApplyEffect<Freeze>(null, player);
            }
        }
    }
}