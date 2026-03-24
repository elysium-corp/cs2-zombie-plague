namespace ZPCore.Data.Weapons;

internal interface ICustomWeapon
{
    public string OriginalName { get; }
    public string IternalName { get; }
    public string DisplayName { get; }

    public void Load();
}