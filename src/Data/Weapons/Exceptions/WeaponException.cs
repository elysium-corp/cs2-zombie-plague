namespace CS2ZombiePlague.Data.Weapons.Exceptions;

public class WeaponException : SystemException
{
    protected WeaponException(string? value) : base(value) { }
    
    protected WeaponException() { }

    public sealed class NotAttachedWeaponException : WeaponException
    {
        public NotAttachedWeaponException(string? value) : base(value) { }
        
        public NotAttachedWeaponException() { }
    }
}