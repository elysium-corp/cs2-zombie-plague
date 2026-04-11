namespace CustomEquipment.Exceptions;

public class NotAttachedGrenadeException : SystemException
{
    public NotAttachedGrenadeException(string? value) : base(value) { }
        
    public NotAttachedGrenadeException() { }
}