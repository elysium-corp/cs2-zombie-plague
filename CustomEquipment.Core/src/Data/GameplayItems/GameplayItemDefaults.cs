using CustomEquipment.Api.Enums;
using CustomEquipment.Data.Equipments.Weapons;

namespace CustomEquipment.Data.GameplayItems;

internal static class GameplayItemKeys
{
    public const string BarrierNade = "barrier_nade";
    public const string FireNade = "fire_nade";
    public const string FrostNade = "frost_nade";
    public const string JumpNade = "jump_nade";
    public const string ShakeNade = "shake_nade";
    public const string LaserMine = "laser_mine";
}

internal static class GameplayItemDefaults
{
    private static readonly IReadOnlyDictionary<string, GameplayItemDefinition> Definitions =
        new Dictionary<string, GameplayItemDefinition>(StringComparer.Ordinal)
        {
            [GameplayItemKeys.BarrierNade] = new(
                GameplayItemKeys.BarrierNade,
                "custom_equipment:barrier_nade",
                "Barrier Nade",
                "Equipment.Item.custom_equipment.barrier_nade.Name",
                WeaponName.Smoke,
                AccessFlags.Human,
                ItemRarity.Rare,
                "weapons/luci/elysium_smoke/elysium_smoke_ag2.vmdl",
                100,
                true,
                10,
                new BarrierNadeSettings(
                    "particles/barrier_nade.vpcf",
                    "ZombiePlague.barrier_impact",
                    "ZombiePlague.barrier_environment",
                    0.65f,
                    175f,
                    15f,
                    0.05f,
                    200f,
                    150f,
                    25f
                )
            ),
            [GameplayItemKeys.FireNade] = new(
                GameplayItemKeys.FireNade,
                "custom_equipment:fire_nade",
                "Fire Nade",
                "Equipment.Item.custom_equipment.fire_nade.Name",
                WeaponName.Inc,
                AccessFlags.Human,
                ItemRarity.Uncommon,
                "weapons/luci/incenderiary_gren/incenderiary_gren_ag2.vmdl",
                100,
                true,
                20,
                new FireNadeSettings(275f, 8f, 2f, 5f)
            ),
            [GameplayItemKeys.FrostNade] = new(
                GameplayItemKeys.FrostNade,
                "custom_equipment:frost_nade",
                "Frost Nade",
                "Equipment.Item.custom_equipment.frost_nade.Name",
                WeaponName.He,
                AccessFlags.Human,
                ItemRarity.Rare,
                "weapons/luci/sifi_hegrenade/sifi_hegrenade_ag2.vmdl",
                100,
                true,
                30,
                new FrostNadeSettings(250f, 5f, 0.1f)
            ),
            [GameplayItemKeys.JumpNade] = new(
                GameplayItemKeys.JumpNade,
                "custom_equipment:jump_nade",
                "Jump Nade",
                "Equipment.Item.custom_equipment.jump_nade.Name",
                WeaponName.He,
                AccessFlags.Zombie,
                ItemRarity.Uncommon,
                "models/throwhead/throwhead2_ag2.vmdl",
                100,
                true,
                40,
                new JumpNadeSettings(250f, 1050f)
            ),
            [GameplayItemKeys.ShakeNade] = new(
                GameplayItemKeys.ShakeNade,
                "custom_equipment:shake_nade",
                "Shake Nade",
                "Equipment.Item.custom_equipment.shake_nade.Name",
                WeaponName.Smoke,
                AccessFlags.Zombie,
                ItemRarity.Rare,
                "models/throwhead/throwhead_ag2.vmdl",
                100,
                true,
                50,
                new ShakeNadeSettings(250f, 10f)
            ),
            [GameplayItemKeys.LaserMine] = new(
                GameplayItemKeys.LaserMine,
                "custom_equipment:laser_mine",
                "Laser Mine",
                "Equipment.Item.custom_equipment.laser_mine.Name",
                WeaponName.C4,
                AccessFlags.Human,
                ItemRarity.Rare,
                "models/lasermine.vmdl",
                100,
                true,
                60,
                new LaserMineSettings(
                    "models/lasermine.vmdl",
                    0.15f,
                    35f,
                    2000f,
                    100,
                    0.5f,
                    0,
                    0,
                    255,
                    255,
                    100f,
                    1f,
                    100
                )
            )
        };

    public static IReadOnlyCollection<string> ImplementationKeys => Definitions.Keys.ToArray();

    public static IReadOnlyCollection<GameplayItemDefinition> All => Definitions.Values.ToArray();

    public static GameplayItemDefinition Get(string implementationKey)
    {
        return Definitions.TryGetValue(implementationKey, out var definition)
            ? definition
            : throw new InvalidOperationException($"Unknown gameplay item implementation '{implementationKey}'.");
    }
}
