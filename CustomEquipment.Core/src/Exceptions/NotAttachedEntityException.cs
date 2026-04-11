namespace CustomEquipment.Exceptions;

internal sealed class NotAttachedEntityException : SystemException
{
    public NotAttachedEntityException(string? value) : base(value) { }
        
    public NotAttachedEntityException() { }
}