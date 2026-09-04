using ZombiePlague.Api.Data;

namespace CustomKnife.Data.Knives;

internal static class KnifeDefaults
{
    public const string DefaultKnifeId = "knife_axe";

    public static readonly KnifeDefinition Fallback = new(
        Enabled: true,
        InternalName: DefaultKnifeId,
        DisplayName: "Axe",
        DisplayNameKey: "CustomKnife.Knife.Axe.Name",
        Model: "weapons/nozb1/valogun/knife/ashen_kukri/ashen_kukri_ag2.vmdl",
        Description: "Гравитация",
        DescriptionKey: "CustomKnife.Knife.Axe.Description",
        Speed: 250f,
        KnockbackData: new KnockbackData(250f, 150f),
        Gravity: 600,
        DamageMultiplier: 1f,
        RequiredPermission: null
    );
}
