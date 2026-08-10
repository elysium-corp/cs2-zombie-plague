namespace CustomEquipment.Api.Exceptions;

public sealed class NotAttachedGrenadeException : NotAttachedEntityException
{
    public NotAttachedGrenadeException(string? value) : base(value) { }
        
    public NotAttachedGrenadeException() { }
}