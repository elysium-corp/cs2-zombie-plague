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

internal sealed class FireNade : GrenadeItemBase
{
    private const float BurnRadius = 275.0f;

    private readonly BurnSettings _settings = new(8.0f, 2.0f, 5.0f);
    
    public override string InheritorName => WeaponName.Inc;
    
    public override AccessFlags AccessFlags => AccessFlags.Human;

    public override string DisplayName => "Fire Nade";
    
    public override string InternalName => "custom_equipment:fire_nade";

    public override Slot Slot => Slot.Grenade;

    public override WeaponType WeaponType => WeaponType.Grenade;

    public override string Model => "weapons/luci/incenderiary_gren/incenderiary_gren_ag2.vmdl";

    public override void OnDetonate(IPlayer thrower, Vector position)
    {
        var core = DependencyResolver.GetRequiredService<ISwiftlyCore>();
        var effectService = EffectService.Provide(core);
        var alivePlayers = core.PlayerManager.GetTAlive();

        var players = Geometry.FindPlayersInSphere(alivePlayers, BurnRadius, position);

        foreach (var player in players) effectService.ApplyEffect<Burn>(thrower, player, _settings);
    }
}