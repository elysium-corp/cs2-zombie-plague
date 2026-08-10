using Common.Di;
using Common.Effects;
using Common.Effects.Effects;
using Common.Effects.Effects.Settings;
using Common.Math;
using CustomEquipment.Api.Data;
using CustomEquipment.Api.Enums;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Data.Equipments.Weapons.Grenades;

public class ShakeNade : GrenadeItemBase
{
    public override string InheritorName => WeaponName.Smoke;

    public override string DisplayName => "Shake Nade";
    
    public override string InternalName => "custom_equipment:shake_nade";
    
    public override Slot Slot => Slot.Grenade;

    public override WeaponType WeaponType => WeaponType.Grenade;

    public override string Model => "weapons/luci/sifi_hegrenade/sifi_hegrenade_ag2.vmdl";

    private readonly DisorientSettings _settings = new DisorientSettings(10.0f);
    
    private const float ShakeRadius = 250.0f;
    
    public override void OnDetonate(IPlayer thrower, Vector position)
    {
        var core = DependencyResolver.GetRequiredService<ISwiftlyCore>();
        var effectService = EffectService.Provide(core);
        var alivePlayers = core.PlayerManager.GetAlive();

        var players = Geometry.FindPlayersInSphere(alivePlayers, ShakeRadius, position);
        
        foreach (var player in players)
        {
            effectService.ApplyEffect<Disorient>(thrower, player, _settings);
        }
    }
}