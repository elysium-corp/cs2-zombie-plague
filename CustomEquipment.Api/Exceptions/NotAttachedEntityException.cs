namespace CustomEquipment.Api.Exceptions;

public class NotAttachedEntityException : InvalidOperationException
{
    public NotAttachedEntityException(string? value) : base(value) { }
        
    public NotAttachedEntityException() { }
}