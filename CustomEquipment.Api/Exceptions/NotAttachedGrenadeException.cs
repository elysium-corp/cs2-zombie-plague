namespace CustomEquipment.Api.Exceptions;

public class NotAttachedGrenadeException : KeyNotFoundException
{
    public NotAttachedGrenadeException(string? value) : base(value) { }
        
    public NotAttachedGrenadeException() { }
}