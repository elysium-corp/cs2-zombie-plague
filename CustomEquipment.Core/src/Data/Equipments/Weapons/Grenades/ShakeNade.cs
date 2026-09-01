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

/// <summary>
/// Представляет оглушающую гранату с параметрами из PostgreSQL-каталога.
/// </summary>
public class ShakeNade : ManagedGrenadeItemBase
{
    /// <summary>
    /// Создаёт гранату со встроенными параметрами по умолчанию.
    /// </summary>
    public ShakeNade() : this(new GameplayItemCatalog())
    {
    }

    /// <summary>
    /// Создаёт гранату с параметрами из указанного runtime-каталога.
    /// </summary>
    public ShakeNade(GameplayItemCatalog catalog)
        : base(catalog, GameplayItemKeys.ShakeNade)
    {
    }

    private ShakeNadeSettings Settings => (ShakeNadeSettings)Definition.Settings;

    public override void OnDetonate(IPlayer thrower, Vector position)
    {
        var core = DependencyResolver.GetRequiredService<ISwiftlyCore>();
        var effectService = EffectService.Provide(core);
        var settings = Settings;
        var alivePlayers = core.PlayerManager.GetCTAlive();
        var players = Geometry.FindPlayersInSphere(alivePlayers, settings.Radius, position);
        var disorientSettings = new DisorientSettings(settings.Duration);

        foreach (var player in players)
        {
            effectService.ApplyEffect<Disorient>(thrower, player, disorientSettings);
        }
    }
}
