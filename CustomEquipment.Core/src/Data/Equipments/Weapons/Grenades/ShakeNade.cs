using Common.Di;
using Common.Effects;
using Common.Effects.Effects;
using Common.Math;
using CustomEquipment.Data.Equipments.Contracts;
using CustomEquipment.Data.Equipments.Enums;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Data.Equipments.Weapons.Grenades;

public class ShakeNade : BaseGrenade
{
    public override string InheritorName => WeaponName.Smoke;

    public override string DisplayName => "Shake Nade";
    
    public override Slot Slot => Slot.Grenade;

    public override WeaponType WeaponType => WeaponType.Grenade;

    public override string Model => "weapons/luci/sifi_hegrenade/sifi_hegrenade_ag2.vmdl";

    private const float ShakeRadius = 250.0f;
    
    public override void OnDetonate(IPlayer thrower, Vector position)
    {
        var core = DependencyResolver.GetRequiredService<ISwiftlyCore>();
        var effectService = EffectService.Provide(core);
        var alivePlayers = core.PlayerManager.GetAlive();

        var players = Geometry.FindPlayersInSphere(alivePlayers, ShakeRadius, position);
        
        foreach (var player in players)
        {
            effectService.ApplyEffect<Disorient>(thrower, player);
        }
    }
}