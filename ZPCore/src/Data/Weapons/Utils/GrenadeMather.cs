namespace ZPCore.Data.Weapons.Utils;

internal static class GrenadeMather
{
    public static bool IsMatched(string name)
    {
        return WeaponName.Grenade.Grenades.Find(name.Contains) != null;
    }

    public static bool IsNotMatched(string name)
    {
        return !IsMatched(name);
    }

    public static string? IfMatchedThenPrimitive(string name)
    {
        return WeaponName.Grenade.Grenades.Find(name.Contains);
    }
}