namespace CustomEquipment.Api.Exceptions;

public class NotAttachedGrenadeException : SystemException
{
    public NotAttachedGrenadeException(string? value) : base(value) { }
        
    public NotAttachedGrenadeException() { }
}