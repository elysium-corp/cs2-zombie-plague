using Common.Di;
using Common.Effects;
using Common.Effects.Contracts;
using Common.Effects.Effects;
using Common.Math;
using CustomEquipment.Data.Equipments.Contracts;
using CustomEquipment.Data.Equipments.Enums;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Data.Equipments.Weapons.Grenades;

internal sealed class FireNade : BaseGrenade
{
    public override string InheritorName => WeaponName.Inc;

    public override string DisplayName => "Fire Nade";
    
    public override Slot Slot => Slot.Grenade;

    public override WeaponType WeaponType => WeaponType.Grenade;

    public override string Model => "weapons/luci/incenderiary_gren/incenderiary_gren_ag2.vmdl";
    
    private const float BurnRadius = 275.0f;

    public override void OnDetonate(IPlayer thrower, Vector position)
    {
        var core = DependencyResolver.GetRequiredService<ISwiftlyCore>();
        var effectService = EffectService.Provide(core);
        var alivePlayers = core.PlayerManager.GetTAlive();
        
        var players = Geometry.FindPlayersInSphere(alivePlayers, BurnRadius, position);

        foreach (var player in players)
        {
            effectService.ApplyEffect<Burn>(thrower, player);
        }
    }
}