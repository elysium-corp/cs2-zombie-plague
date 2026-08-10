namespace CustomEquipment.Api.Exceptions;

public sealed class NotAttachedEntityException : KeyNotFoundException
{
    public NotAttachedEntityException(string? value) : base(value) { }
        
    public NotAttachedEntityException() { }
}