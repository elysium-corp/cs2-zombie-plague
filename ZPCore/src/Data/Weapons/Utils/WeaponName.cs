namespace ZPCore.Data.Weapons.Utils;

internal static class WeaponName
{
    public static class Grenade
    {
        public const string Decoy = "decoy";
        public const string He = "hegrenade";
        public const string Inc = "incgrenade";
        public const string Molotov = "molotov";
        public const string Smoke = "smoke";

        public static readonly List<string> Grenades = [Decoy, He, Inc, Molotov, Smoke];
    }
}