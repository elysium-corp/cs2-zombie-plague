namespace CustomEquipment.Api.Exceptions;

public sealed class CannotCreateItemException : SystemException
{
    public CannotCreateItemException(string? value) : base(value) { }
        
    public CannotCreateItemException() { }
}