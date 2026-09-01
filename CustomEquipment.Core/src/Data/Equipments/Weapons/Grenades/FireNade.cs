using Common.Di;
using Common.Effects;
using Common.Effects.Effects;
using Common.Effects.Effects.Settings;
using Common.Math;
using CustomEquipment.Data.GameplayItems;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Data.Equipments.Weapons.Grenades;

internal sealed class FireNade(GameplayItemCatalog catalog)
    : ManagedGrenadeItemBase(catalog, GameplayItemKeys.FireNade)
{
    private FireNadeSettings Settings => (FireNadeSettings)Definition.Settings;

    public override void OnDetonate(IPlayer thrower, Vector position)
    {
        var core = DependencyResolver.GetRequiredService<ISwiftlyCore>();
        var effectService = EffectService.Provide(core);
        var settings = Settings;
        var alivePlayers = core.PlayerManager.GetTAlive();
        var players = Geometry.FindPlayersInSphere(alivePlayers, settings.Radius, position);
        var burnSettings = new BurnSettings(
            settings.Duration,
            settings.DamagePerTickPercent,
            settings.InstantDamagePercent
        );

        foreach (var player in players)
        {
            effectService.ApplyEffect<Burn>(thrower, player, burnSettings);
        }
    }
}
