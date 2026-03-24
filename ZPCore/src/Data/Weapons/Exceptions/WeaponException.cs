namespace ZPCore.Data.Weapons.Exceptions;

internal class WeaponException : SystemException
{
    protected WeaponException(string? value) : base(value) { }
    
    protected WeaponException() { }

    public sealed class NotAttachedWeaponException : WeaponException
    {
        public NotAttachedWeaponException(string? value) : base(value) { }
        
        public NotAttachedWeaponException() { }
    }
}