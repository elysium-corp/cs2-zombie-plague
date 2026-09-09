using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CustomEquipment.Api.Data.Contracts;

namespace CustomEquipment.Api.Utils;

/// <summary>Общие правила адресации VSVG для магазина и других пользовательских HUD.</summary>
public static partial class EquipmentHudIcon
{
    /// <summary>Проверяет путь ресурса без URI, обхода каталогов и CSS-разметки.</summary>
    public static string? NormalizePath(string? path)
    {
        path = path?.Trim();
        return path is { Length: <= 512 } && ResourcePath().IsMatch(path) ? path : null;
    }

    /// <summary>Имя CSS-класса в пакете иконок, экспортируемом из CMS.</summary>
    public static string CssName(string path)
    {
        if (NormalizePath(path) is not { } normalized)
            throw new ArgumentException("Недопустимый путь VSVG", nameof(path));
        return "custom_" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)))[..32];
    }

    /// <summary>Выбирает собственный ресурс, затем VSVG базового оружия. Проверка существования необязательна.</summary>
    public static string Resolve(IItem item, Func<string, bool>? resourceExists = null)
    {
        if (item is IHasHudIcon icon && NormalizePath(icon.HudIconPath) is { } custom &&
            (resourceExists is null || resourceExists(custom + "_c"))) return custom;
        var name = item switch
        {
            IWeapon weapon => weapon.InheritorName,
            IGrenade grenade => grenade.InheritorName,
            _ when item.Slot == Enums.Slot.Knife => "knife",
            _ => "kevlar"
        };
        name = name.Trim().ToLowerInvariant();
        if (name.StartsWith("weapon_", StringComparison.Ordinal)) name = name[7..];
        if (!BaseName().IsMatch(name)) name = "kevlar";
        return "panorama/images/icons/equipment/" + name + ".vsvg";
    }

    [GeneratedRegex(@"\Apanorama/images/(?:[a-z0-9_-]+/)*[a-z0-9_-]+\.vsvg\z", RegexOptions.CultureInvariant)]
    private static partial Regex ResourcePath();

    [GeneratedRegex(@"\A[a-z0-9_]{1,64}\z", RegexOptions.CultureInvariant)]
    private static partial Regex BaseName();
}
