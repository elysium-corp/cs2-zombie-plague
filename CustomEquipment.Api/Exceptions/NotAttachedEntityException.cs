namespace CustomEquipment.Api.Exceptions;

public sealed class NotAttachedEntityException : SystemException
{
    public NotAttachedEntityException(string? value) : base(value) { }
        
    public NotAttachedEntityException() { }
}