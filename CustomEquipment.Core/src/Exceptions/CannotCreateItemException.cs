namespace CustomEquipment.Exceptions;

internal sealed class CannotCreateItemException : SystemException
{
    public CannotCreateItemException(string? value) : base(value) { }
        
    public CannotCreateItemException() { }
}